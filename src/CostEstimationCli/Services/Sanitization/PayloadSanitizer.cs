using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CostEstimationCli.Services.Sanitization;

/// <summary>
/// Two-pass sanitizer that anonymises sensitive data in IaC deployment JSON
/// while preserving the structural dependency graph and all pricing metadata.
///
/// <para>
/// <b>Pass 1 – Discovery:</b> Scans every string value in the document to
/// build two deterministic lookup maps:
/// <list type="bullet">
///   <item>
///     <b>URN map</b> — each unique Pulumi URN is assigned a sequential,
///     type-derived token (e.g.
///     <c>urn:pulumi:prod::AcmeCorp::azure-native:sql:Database::customer-db</c>
///     → <c>res-sql-database-01</c>).
///   </item>
///   <item>
///     <b>GUID map</b> — each unique UUID/GUID found in non-URN strings is
///     assigned a zero-padded generic GUID
///     (e.g. <c>a1b2c3d4-…</c> → <c>00000000-0000-0000-0000-000000000001</c>).
///   </item>
/// </list>
/// </para>
///
/// <para>
/// <b>Pass 2 – Transformation:</b> Re-traverses the document using three
/// classification sets to decide how each key/value pair is treated:
/// <list type="bullet">
///   <item>
///     <b>Pricing whitelist</b> — the entire value subtree is deep-cloned
///     without modification (location, SKU, vmSize, vCores, count, etc.).
///   </item>
///   <item>
///     <b>Graph / structural keys</b> (<c>urn</c>, <c>parent</c>,
///     <c>provider</c>, <c>dependencies</c>, <c>propertyDependencies</c>) —
///     values are replaced via the URN/GUID lookup maps so every edge in the
///     dependency graph continues to reference the correct anonymous token.
///   </item>
///   <item>
///     <b>Sensitive keys</b> (<c>resourceGroupName</c>, <c>serverName</c>,
///     <c>adminPassword</c>, etc.) — string values are unconditionally
///     replaced with <c>[redacted]</c>.
///   </item>
/// </list>
/// Unclassified string leaf values are passed through
/// <see cref="TransformValue"/> which replaces any embedded GUIDs via the
/// GUID map and leaves all other strings unchanged.
/// </para>
/// </summary>
public sealed class PayloadSanitizer : IPayloadSanitizer
{
    // -------------------------------------------------------------------------
    // Compiled regex — reused for every call
    // -------------------------------------------------------------------------

    private static readonly Regex GuidRegex = new(
        @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly JsonSerializerOptions PrettyPrintOptions =
        new() { WriteIndented = true };

    // -------------------------------------------------------------------------
    // Key classification sets
    // -------------------------------------------------------------------------

    /// <summary>
    /// Keys whose <em>entire value subtree</em> is preserved verbatim.
    /// These contain all data the pricing engine needs to produce estimates.
    /// </summary>
    private static readonly HashSet<string> PricingWhitelistKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Non-sensitive structural flags
            "op", "custom", "type", "__createBeforeDelete",

            // Geography
            "location",

            // SKU hierarchy — tier / family / capacity live inside this subtree
            "sku",

            // Compute
            "vmSize", "hardwareProfile",

            // Database / managed instance sizing
            "vCores", "storageSizeInGB", "memorySizeInGB", "storageIOps",
            "isGeneralPurposeV2", "zoneRedundant", "licenseType",
            "requestedBackupStorageRedundancy",
            "estimatedBillablePitrBackupStorageInGB",
            "estimatedLtrBackupStorageInGB",

            // Kubernetes / AKS — entire agentPoolProfiles array is whitelisted so
            // node-pool names (agentPoolProfiles[].name) are preserved for the API.
            "agentPoolProfiles",
            "count", "nodeCount", "kubernetesVersion", "osType",

            // VM scheduling
            "priority",

            // OS image — offer/publisher/sku/version determine OS pricing
            "imageReference", "offer", "publisher", "version",

            // Disk storage tier
            "createOption", "managedDisk", "storageAccountType",

            // OS configuration blocks (signal OS type; contain no secrets at
            // this nesting level when linuxConfiguration / windowsConfiguration
            // are the keys)
            "linuxConfiguration", "windowsConfiguration",
            "enableAutomaticUpdates", "provisionVMAgent",
            "patchSettings", "assessmentMode",
        };

    /// <summary>
    /// Keys whose string values are always replaced with <c>[redacted]</c>.
    /// </summary>
    private static readonly HashSet<string> SensitiveKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "resourceGroupName",
            "serverName",
            "databaseName",
            "managedInstanceName",
            "resourceName",
            "computerName",
            "vmName",
            "dnsPrefix",
            "adminPassword",
            "adminUsername",
            // "name" covers agent-pool names, OS-disk names, etc.
            // sku.name is safe because the whole "sku" subtree is whitelisted
            // and is therefore never recursed into.
            "name",
        };

    /// <summary>
    /// Keys that are stripped entirely from the output to reduce payload size.
    /// These fields carry no pricing or structural information that the
    /// estimation engine consumes.
    /// </summary>
    private static readonly HashSet<string> DroppedKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Pulumi diff telemetry — always null on preview, and very large
            // on update plans; not consumed by the pricing engine.
            "detailedDiff",
        };

    /// <summary>
    /// Keys whose string values are URNs that must be mapped through the
    /// anonymisation lookup table to preserve directed graph edges.
    /// </summary>
    private static readonly HashSet<string> GraphUrnKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "urn", "parent", "provider",
        };

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public string Sanitize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var root = JsonNode.Parse(json)
            ?? throw new ArgumentException("Input JSON parsed to null.", nameof(json));

        // ---- Pass 1: build lookup maps -------------------------------------
        var urnMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var typeCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var guidCounter = 0;

        CollectIdentifiers(root, urnMap, guidMap, typeCounters, ref guidCounter);

        // ---- Pass 2: transform --------------------------------------------
        var sanitized = TransformNode(root, parentKey: null, urnMap, guidMap);

        return sanitized?.ToJsonString(PrettyPrintOptions) ?? "{}";
    }

    // =========================================================================
    // Pass 1 — Identifier Discovery
    // =========================================================================

    private static void CollectIdentifiers(
        JsonNode? node,
        Dictionary<string, string> urnMap,
        Dictionary<string, string> guidMap,
        Dictionary<string, int> typeCounters,
        ref int guidCounter)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var kv in obj)
                    CollectIdentifiers(kv.Value, urnMap, guidMap, typeCounters, ref guidCounter);
                break;

            case JsonArray arr:
                foreach (var item in arr)
                    CollectIdentifiers(item, urnMap, guidMap, typeCounters, ref guidCounter);
                break;

            case JsonValue val when val.GetValueKind() == JsonValueKind.String:
            {
                var str = val.GetValue<string>();
                if (string.IsNullOrEmpty(str)) break;

                if (str.StartsWith("urn:pulumi:", StringComparison.OrdinalIgnoreCase))
                {
                    // Register the full URN once; the entire string is the key.
                    if (!urnMap.ContainsKey(str))
                        urnMap[str] = GenerateUrnToken(str, typeCounters);
                }
                else
                {
                    // Register every GUID found inside non-URN strings so they
                    // can be replaced in Pass 2 (e.g. subscription IDs used as
                    // resource-group names or injected via env-vars).
                    foreach (Match m in GuidRegex.Matches(str))
                    {
                        var key = m.Value.ToLowerInvariant();
                        if (!guidMap.ContainsKey(key))
                            guidMap[key] = $"00000000-0000-0000-0000-{++guidCounter:D12}";
                    }
                }
                break;
            }
        }
    }

    // =========================================================================
    // URN token generation
    // =========================================================================

    private static string GenerateUrnToken(
        string urn,
        Dictionary<string, int> typeCounters)
    {
        var type = ExtractTypeFromUrn(urn);
        var slug = SlugFromType(type);

        // Stack and provider meta-types stay as opaque countered tokens because
        // their last URN segment (project name, provider GUID) is sensitive.
        if (slug == "stack" || slug.StartsWith("provider-", StringComparison.OrdinalIgnoreCase))
        {
            typeCounters.TryGetValue(slug, out var count);
            typeCounters[slug] = ++count;
            return slug == "stack" ? $"stack-{count:D2}" : $"{slug}-{count:D2}";
        }

        // For regular cloud resources, use the logical name from the URN's last
        // segment (e.g. "test-rg", "sandbox-aks-cfy"). This keeps unsupportedResources
        // names consistent with the naming convention used everywhere else and makes
        // the output readable to the customer. Pulumi guarantees URN uniqueness within
        // a stack, so logical names are safe collision-free tokens.
        // URN format: urn:pulumi:{stack}::{project}::{type}::{name}
        var segments = urn.Split("::");
        if (segments.Length >= 4)
            return segments[^1];

        // Fallback: type-based opaque token
        typeCounters.TryGetValue(slug, out var cnt);
        typeCounters[slug] = ++cnt;
        return $"res-{slug}-{cnt:D2}";
    }

    /// <summary>
    /// Extracts the resource-type segment from a Pulumi URN.
    /// URN format: <c>urn:pulumi:{stack}::{project}::{type}::{name}[::…]</c>
    /// </summary>
    private static string ExtractTypeFromUrn(string urn)
    {
        // Splitting on "::" yields: [0] "urn:pulumi:{stack}", [1] "{project}",
        //                            [2] "{type}", [3] "{name}", [4+] extras
        var segments = urn.Split("::");
        return segments.Length >= 3 ? segments[2] : "unknown";
    }

    /// <summary>
    /// Converts a Pulumi resource-type string into a kebab-case slug used as
    /// the base for anonymous token names.
    /// </summary>
    /// <example>
    /// <code>
    /// "pulumi:pulumi:Stack"                          → "stack"
    /// "pulumi:providers:azure-native"                → "provider-azure-native"
    /// "azure-native:sql:Database"                    → "sql-database"
    /// "azure-native:containerservice:ManagedCluster" → "containerservice-managedcluster"
    /// </code>
    /// </example>
    private static string SlugFromType(string type)
    {
        if (type.Equals("pulumi:pulumi:Stack", StringComparison.OrdinalIgnoreCase))
            return "stack";

        var parts = type.Split(':');

        if (parts.Length >= 3
            && parts[0].Equals("pulumi", StringComparison.OrdinalIgnoreCase)
            && parts[1].Equals("providers", StringComparison.OrdinalIgnoreCase))
        {
            // pulumi:providers:azure-native → "provider-azure-native"
            return "provider-" + string.Join("-", parts.Skip(2)).ToLowerInvariant();
        }

        // azure-native:sql:Database          → skip first segment → "sql-database"
        // azure-native:containerservice:ManagedCluster → "containerservice-managedcluster"
        return string.Join("-", parts.Skip(1)).ToLowerInvariant();
    }

    // =========================================================================
    // Pass 2 — Graph-preserving Transformation
    // =========================================================================

    private static JsonNode? TransformNode(
        JsonNode? node,
        string? parentKey,
        IReadOnlyDictionary<string, string> urnMap,
        IReadOnlyDictionary<string, string> guidMap)
    {
        return node switch
        {
            JsonObject obj => TransformObject(obj, urnMap, guidMap),
            JsonArray  arr => TransformArray(arr, parentKey, urnMap, guidMap),
            JsonValue  val => TransformValue(val, urnMap, guidMap),
            null           => null,
            _              => node.DeepClone(),
        };
    }

    private static JsonObject TransformObject(
        JsonObject obj,
        IReadOnlyDictionary<string, string> urnMap,
        IReadOnlyDictionary<string, string> guidMap)
    {
        var result = new JsonObject();

        foreach (var (key, value) in obj)
        {
            // -----------------------------------------------------------------
            // 0. Dropped keys — strip entirely to reduce payload size
            // -----------------------------------------------------------------
            if (DroppedKeys.Contains(key))
                continue;

            // Null JSON values are preserved as-is
            if (value is null)
            {
                result[key] = null;
                continue;
            }

            // -----------------------------------------------------------------
            // 1. Pricing whitelist — deep-clone the entire subtree unchanged
            // -----------------------------------------------------------------
            if (PricingWhitelistKeys.Contains(key))
            {
                result[key] = value.DeepClone();
                continue;
            }

            // -----------------------------------------------------------------
            // 2. Graph / structural URN fields
            // -----------------------------------------------------------------
            if (GraphUrnKeys.Contains(key)
                && value is JsonValue urnVal
                && urnVal.GetValueKind() == JsonValueKind.String)
            {
                var urnStr = urnVal.GetValue<string>();
                result[key] = MapUrnOrGuid(urnStr, urnMap, guidMap);
                continue;
            }

            // -----------------------------------------------------------------
            // 3. dependencies: array of URN strings
            // -----------------------------------------------------------------
            if (key.Equals("dependencies", StringComparison.OrdinalIgnoreCase)
                && value is JsonArray depsArr)
            {
                result[key] = MapUrnArray(depsArr, urnMap, guidMap);
                continue;
            }

            // -----------------------------------------------------------------
            // 4. propertyDependencies: { propertyName: URN[] | null }
            // -----------------------------------------------------------------
            if (key.Equals("propertyDependencies", StringComparison.OrdinalIgnoreCase)
                && value is JsonObject propDeps)
            {
                result[key] = MapPropertyDependencies(propDeps, urnMap, guidMap);
                continue;
            }

            // -----------------------------------------------------------------
            // 5. Sensitive keys — string values are unconditionally redacted
            // -----------------------------------------------------------------
            if (SensitiveKeys.Contains(key))
            {
                // Plain string → redact.  Object / array (defensive) → recurse
                // so nested pricing fields are still preserved.
                result[key] = value is JsonValue
                    ? JsonValue.Create("[redacted]")
                    : TransformNode(value, key, urnMap, guidMap);
                continue;
            }

            // -----------------------------------------------------------------
            // 6. Default — recurse; leaf strings are sanitised by TransformValue
            // -----------------------------------------------------------------
            result[key] = TransformNode(value, key, urnMap, guidMap);
        }

        return result;
    }

    private static JsonArray TransformArray(
        JsonArray arr,
        string? parentKey,
        IReadOnlyDictionary<string, string> urnMap,
        IReadOnlyDictionary<string, string> guidMap)
    {
        var result = new JsonArray();
        foreach (var item in arr)
            result.Add(TransformNode(item, parentKey, urnMap, guidMap));
        return result;
    }

    /// <summary>
    /// Sanitises a single JSON string leaf value:
    /// <list type="bullet">
    ///   <item>Full Pulumi URN → replaced via the URN map.</item>
    ///   <item>String that embeds one or more GUIDs → GUIDs are substituted
    ///         via the GUID map; surrounding text is preserved.</item>
    ///   <item>All other strings → returned unchanged (non-identifying values
    ///         such as <c>"Regular"</c>, <c>"Linux"</c>, <c>"FromImage"</c>).
    ///   </item>
    /// </list>
    /// Non-string JSON values (numbers, booleans, null) are always deep-cloned
    /// without modification.
    /// </summary>
    private static JsonNode? TransformValue(
        JsonValue val,
        IReadOnlyDictionary<string, string> urnMap,
        IReadOnlyDictionary<string, string> guidMap)
    {
        if (val.GetValueKind() != JsonValueKind.String)
            return val.DeepClone();

        var str = val.GetValue<string>();

        // Full Pulumi URN
        if (str.StartsWith("urn:pulumi:", StringComparison.OrdinalIgnoreCase))
            return JsonValue.Create(MapUrn(str, urnMap));

        // Strings that contain at least one GUID (standalone or embedded)
        if (GuidRegex.IsMatch(str))
        {
            var replaced = GuidRegex.Replace(str, m =>
            {
                var lower = m.Value.ToLowerInvariant();
                return guidMap.TryGetValue(lower, out var token)
                    ? token
                    : "[redacted-guid]";
            });
            return JsonValue.Create(replaced);
        }

        // Non-identifying string — preserve as-is
        return val.DeepClone();
    }

    // =========================================================================
    // Mapping helpers
    // =========================================================================

    /// <summary>
    /// Looks up <paramref name="value"/> in the URN map first, then falls back
    /// to GUID map / GUID-regex replacement, then returns <c>[redacted]</c>.
    /// Empty / null values are returned unchanged (they represent absent parents).
    /// </summary>
    private static string MapUrnOrGuid(
        string value,
        IReadOnlyDictionary<string, string> urnMap,
        IReadOnlyDictionary<string, string> guidMap)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (urnMap.TryGetValue(value, out var urnToken))
            return urnToken;

        if (Guid.TryParse(value, out _))
        {
            var key = value.ToLowerInvariant();
            if (guidMap.TryGetValue(key, out var guidToken))
                return guidToken;
        }

        if (GuidRegex.IsMatch(value))
        {
            return GuidRegex.Replace(value, m =>
            {
                var key = m.Value.ToLowerInvariant();
                return guidMap.TryGetValue(key, out var t) ? t : "[redacted-guid]";
            });
        }

        return "[redacted]";
    }

    private static string MapUrn(string urn, IReadOnlyDictionary<string, string> urnMap)
        => urnMap.TryGetValue(urn, out var token) ? token : "[redacted-urn]";

    private static JsonArray MapUrnArray(
        JsonArray arr,
        IReadOnlyDictionary<string, string> urnMap,
        IReadOnlyDictionary<string, string> guidMap)
    {
        var result = new JsonArray();
        foreach (var item in arr)
        {
            if (item is JsonValue itemVal
                && itemVal.GetValueKind() == JsonValueKind.String)
            {
                result.Add(JsonValue.Create(
                    MapUrnOrGuid(itemVal.GetValue<string>(), urnMap, guidMap)));
            }
            else
            {
                result.Add(item?.DeepClone());
            }
        }
        return result;
    }

    private static JsonObject MapPropertyDependencies(
        JsonObject propDeps,
        IReadOnlyDictionary<string, string> urnMap,
        IReadOnlyDictionary<string, string> guidMap)
    {
        var result = new JsonObject();
        foreach (var (propKey, propValue) in propDeps)
        {
            // Skip null and empty arrays — they carry no dependency edges and
            // are a significant source of payload bloat on large stacks.
            if (propValue is not JsonArray urnArr || urnArr.Count == 0)
                continue;

            result[propKey] = MapUrnArray(urnArr, urnMap, guidMap);
        }
        return result;
    }
}

using System.Text.Json;
using CostEstimationCli.Models;
using CostEstimationCli.Repositories;
using Microsoft.Extensions.Logging;

namespace CostEstimationCli.Services;

/// <summary>
/// Computes cost deltas from Pulumi preview step operations by grouping resource
/// steps by their <c>op</c> field (create / delete / update / same) and making a
/// targeted API call for the "create" group to determine added monthly spend.
///
/// <para>
/// Delta for deleted resources is not available from the preview JSON alone
/// (pre-existing pricing data would be required), so <see cref="CostDelta.SavedMonthly"/>
/// is always reported as 0 and the deleted resource count is surfaced instead.
/// </para>
/// </summary>
public class DeltaCalculationService
{
    private readonly IApiRepository _apiRepository;
    private readonly ILogger<DeltaCalculationService> _logger;

    public DeltaCalculationService(
        IApiRepository apiRepository,
        ILogger<DeltaCalculationService> logger)
    {
        _apiRepository = apiRepository ?? throw new ArgumentNullException(nameof(apiRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parses the raw provider JSON for a Pulumi preview <c>steps</c> array and
    /// computes a <see cref="CostDelta"/>.  Returns <see langword="null"/> when the
    /// JSON does not contain a <c>steps</c> array (e.g. stack-state exports).
    /// </summary>
    public async Task<CostDelta?> ComputeAsync(
        string previewJson,
        IReadOnlyList<string>? ignoreResourceTypes = null,
        IReadOnlyList<string>? ignoreResourceNames = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(previewJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("steps", out var stepsElement) ||
                stepsElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogDebug(
                    "No 'steps' array in preview JSON; delta calculation skipped.");
                return null;
            }

            var createStepRaws = new List<string>();
            int createdCount = 0, deletedCount = 0, updatedCount = 0, unchangedCount = 0;

            foreach (var step in stepsElement.EnumerateArray())
            {
                var op           = step.TryGetProperty("op",  out var opEl)  ? opEl.GetString()  : null;
                var urn          = step.TryGetProperty("urn", out var urnEl) ? urnEl.GetString() : null;
                var resourceType = ExtractTypeFromUrn(urn);
                var resourceName = ExtractNameFromUrn(urn);

                // Skip Pulumi meta-resources (Stack, provider registrations)
                if (IsPulumiMetaResource(urn))
                    continue;

                // Apply ignore-list rules from .saasfactory-cost.json
                if (ShouldIgnore(resourceType, resourceName, ignoreResourceTypes, ignoreResourceNames))
                {
                    _logger.LogDebug("Skipping ignored resource {Urn}.", urn);
                    continue;
                }

                switch (op)
                {
                    case "create":
                        createdCount++;
                        createStepRaws.Add(step.GetRawText());
                        break;
                    case "delete":
                        deletedCount++;
                        break;
                    case "update":
                        updatedCount++;
                        break;
                    case "same":
                        unchangedCount++;
                        break;
                }
            }

            // ── Compute added monthly cost via a targeted API call ──────────────
            decimal addedMonthly = 0;
            if (createStepRaws.Count > 0)
            {
                var createsJson = BuildFilteredJson(root, createStepRaws);
                try
                {
                    var createsEstimate = await _apiRepository.GetCostEstimateAsync(
                        createsJson, cancellationToken);
                    addedMonthly = createsEstimate.aggregateCosts.PerMonth;

                    _logger.LogDebug(
                        "Delta: {Count} create(s) → +${Added:N2}/mo.",
                        createdCount, addedMonthly);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to retrieve cost for created resources; AddedMonthly will be $0.00.");
                }
            }

            return new CostDelta
            {
                AddedMonthly   = addedMonthly,
                SavedMonthly   = 0,          // Pre-existing cost of deleted resources is not available from preview
                CreatedCount   = createdCount,
                DeletedCount   = deletedCount,
                UpdatedCount   = updatedCount,
                UnchangedCount = unchangedCount,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Delta calculation encountered an unexpected error; returning null.");
            return null;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static bool IsPulumiMetaResource(string? urn) =>
        !string.IsNullOrEmpty(urn) &&
        (urn.Contains("pulumi:pulumi:Stack") || urn.Contains("pulumi:providers:"));

    private static bool ShouldIgnore(
        string? resourceType,
        string? resourceName,
        IReadOnlyList<string>? ignoreTypes,
        IReadOnlyList<string>? ignoreNames)
    {
        if (ignoreTypes?.Count > 0 &&
            !string.IsNullOrEmpty(resourceType) &&
            ignoreTypes.Any(t => t.Equals(resourceType, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (ignoreNames?.Count > 0 &&
            !string.IsNullOrEmpty(resourceName) &&
            ignoreNames.Any(n => n.Equals(resourceName, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Extracts the resource type from a Pulumi URN.
    /// URN format: <c>urn:pulumi:&lt;stack&gt;::&lt;project&gt;::&lt;type&gt;::&lt;name&gt;</c>
    /// </summary>
    private static string? ExtractTypeFromUrn(string? urn)
    {
        if (string.IsNullOrEmpty(urn)) return null;
        var parts = urn.Split("::");
        return parts.Length >= 3 ? parts[^2] : null;
    }

    /// <summary>Extracts the logical resource name (last segment) from a Pulumi URN.</summary>
    private static string? ExtractNameFromUrn(string? urn)
    {
        if (string.IsNullOrEmpty(urn)) return null;
        var parts = urn.Split("::");
        return parts.Length >= 1 ? parts[^1] : null;
    }

    /// <summary>
    /// Rebuilds the Pulumi preview JSON with only the specified steps, keeping all
    /// other top-level properties intact.
    /// </summary>
    private static string BuildFilteredJson(JsonElement root, IList<string> filteredStepRaws)
    {
        using var ms     = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);

        writer.WriteStartObject();

        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name == "steps")
            {
                writer.WritePropertyName("steps");
                writer.WriteStartArray();

                foreach (var stepRaw in filteredStepRaws)
                {
                    using var stepDoc = JsonDocument.Parse(stepRaw);
                    stepDoc.RootElement.WriteTo(writer);
                }

                writer.WriteEndArray();
            }
            else
            {
                prop.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }
}

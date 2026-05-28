namespace CostEstimationCli.Services.Sanitization;

/// <summary>
/// Sanitizes raw Infrastructure-as-Code deployment JSON payloads before they are
/// transmitted to the external pricing API.
///
/// The sanitizer removes all corporate-identifying information — resource names,
/// project names, environment tags, server names, GUIDs, credentials — while
/// preserving two critical aspects of the document:
///
///   1. Structural dependency graph  — every parent reference and every entry
///      inside a <c>dependencies</c> or <c>propertyDependencies</c> array is
///      re-written to point at the anonymised token for the same resource, so
///      the graph topology is identical before and after sanitisation.
///
///   2. Pricing metadata  — location / region, SKU name / tier / family /
///      capacity, VM size, vCores, node counts, storage sizes, Kubernetes
///      version, OS type and licence type are left completely untouched because
///      the pricing engine requires them to produce accurate estimates.
/// </summary>
public interface IPayloadSanitizer
{
    /// <summary>
    /// Processes a raw IaC deployment JSON string (Pulumi preview / Terraform
    /// plan format) and returns a fully anonymised version that is safe to
    /// forward to an external pricing API.
    /// </summary>
    /// <param name="json">
    /// The raw infrastructure JSON to sanitise.  Must not be <see langword="null"/>
    /// or whitespace.
    /// </param>
    /// <returns>
    /// An indented JSON string in which all sensitive identifiers have been
    /// replaced with deterministic, sequential anonymous tokens and all graph
    /// edges remain intact.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="json"/> is <see langword="null"/>, empty or
    /// whitespace, or cannot be parsed as JSON.
    /// </exception>
    string Sanitize(string json);
}

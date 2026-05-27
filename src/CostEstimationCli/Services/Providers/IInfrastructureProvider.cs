namespace CostEstimationCli.Services.Providers;

/// <summary>
/// Interface for infrastructure-as-code providers (Pulumi, Bicep, CDK, etc.)
/// </summary>
public interface IInfrastructureProvider
{
    /// <summary>
    /// Provider name (e.g., "Pulumi", "Bicep", "CDK")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Provider display name with icon
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Checks if this provider can handle the current project
    /// </summary>
    Task<bool> CanHandleAsync(string workingDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts infrastructure resources as JSON to send to API
    /// </summary>
    Task<string> ExtractResourceJsonAsync(CancellationToken cancellationToken = default);
}

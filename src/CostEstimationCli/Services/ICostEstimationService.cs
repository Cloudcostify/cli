using CostEstimationCli.Models;

namespace CostEstimationCli.Services;

/// <summary>
/// Service interface for cost estimation operations
/// </summary>
public interface ICostEstimationService
{
    /// <summary>
    /// Executes the cost estimation process end-to-end.
    /// </summary>
    /// <param name="ignoreResourceTypes">
    /// Optional list of fully-qualified Pulumi resource types to exclude from
    /// the payload (e.g. <c>azure-native:resources:ResourceGroup</c>).
    /// </param>
    /// <param name="ignoreResourceNames">
    /// Optional list of logical resource names (URN last segment) to exclude.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CostEstimateResponseModel> EstimateCostAsync(
        IReadOnlyList<string>? ignoreResourceTypes = null,
        IReadOnlyList<string>? ignoreResourceNames = null,
        CancellationToken cancellationToken = default);
}

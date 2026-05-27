using CostEstimationCli.Models;

namespace CostEstimationCli.Services;

/// <summary>
/// Service interface for cost estimation operations
/// </summary>
public interface ICostEstimationService
{
    /// <summary>
    /// Executes the cost estimation process end-to-end
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cost estimate response model</returns>
    Task<CostEstimateResponseModel> EstimateCostAsync(CancellationToken cancellationToken = default);
}

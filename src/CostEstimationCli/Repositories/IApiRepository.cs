using CostEstimationCli.Models;

namespace CostEstimationCli.Repositories;

/// <summary>
/// Repository interface for API communication
/// </summary>
public interface IApiRepository
{
    /// <summary>
    /// Posts cost estimation data to the API and retrieves the cost estimate
    /// </summary>
    /// <param name="pulumiPreviewJson">JSON data from Pulumi preview</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cost estimate response from the API</returns>
    Task<CostEstimateResponseModel> GetCostEstimateAsync(string pulumiPreviewJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the license/API key
    /// </summary>
    /// <param name="apiKey">API key to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>License validation response</returns>
    Task<LicenseResponseModel> ValidateLicenseAsync(string apiKey, CancellationToken cancellationToken = default);
}

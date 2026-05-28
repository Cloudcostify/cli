using CostEstimationCli.Models;
using CostEstimationCli.Repositories;
using CostEstimationCli.Services.Providers;
using Microsoft.Extensions.Logging;

namespace CostEstimationCli.Services;

/// <summary>
/// Service implementation for cost estimation operations
/// </summary>
public class CostEstimationService : ICostEstimationService
{
    private readonly IInfrastructureProvider _provider;
    private readonly IApiRepository _apiRepository;
    private readonly ILogger<CostEstimationService> _logger;
    private readonly DeltaCalculationService? _deltaService;

    public CostEstimationService(
        IInfrastructureProvider provider,
        IApiRepository apiRepository,
        ILogger<CostEstimationService> logger,
        DeltaCalculationService? deltaService = null)
    {
        _provider      = provider      ?? throw new ArgumentNullException(nameof(provider));
        _apiRepository = apiRepository ?? throw new ArgumentNullException(nameof(apiRepository));
        _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
        _deltaService  = deltaService;
    }

    /// <inheritdoc />
    public async Task<CostEstimateResponseModel> EstimateCostAsync(
        IReadOnlyList<string>? ignoreResourceTypes = null,
        IReadOnlyList<string>? ignoreResourceNames = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Starting cost estimation with provider: {Provider}", _provider.DisplayName);

            // Step 1: Extract infrastructure JSON from provider
            _logger.LogDebug("Extracting infrastructure data using {Provider}", _provider.Name);
            var infrastructureJson = await _provider.ExtractResourceJsonAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(infrastructureJson))
                throw new InvalidOperationException($"{_provider.Name} infrastructure JSON is empty");

            _logger.LogDebug(
                "Infrastructure JSON generated, size: {Size} bytes", infrastructureJson.Length);

            // Step 2: Send full payload to API for cost estimation
            _logger.LogDebug("Requesting cost estimate from API");
            var costEstimate = await _apiRepository.GetCostEstimateAsync(
                infrastructureJson, cancellationToken);

            // Step 3: Compute cost delta when the provider returns a preview (steps array)
            if (_deltaService is not null)
            {
                _logger.LogDebug("Computing cost delta from preview step operations");
                costEstimate.Delta = await _deltaService.ComputeAsync(
                    infrastructureJson,
                    ignoreResourceTypes,
                    ignoreResourceNames,
                    cancellationToken);
            }

            _logger.LogDebug("Cost estimation process completed successfully");
            return costEstimate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cost estimation process failed");
            throw;
        }
    }
}

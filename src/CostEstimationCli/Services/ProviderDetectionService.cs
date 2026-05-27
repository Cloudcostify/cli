using CostEstimationCli.Services.Providers;
using Microsoft.Extensions.Logging;

namespace CostEstimationCli.Services;

/// <summary>
/// Service for detecting which IaC provider to use
/// </summary>
public class ProviderDetectionService
{
    private readonly IEnumerable<IInfrastructureProvider> _providers;
    private readonly ILogger<ProviderDetectionService> _logger;

    public ProviderDetectionService(
        IEnumerable<IInfrastructureProvider> providers,
        ILogger<ProviderDetectionService> logger)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Detects which provider can handle the current working directory
    /// </summary>
    public async Task<IInfrastructureProvider?> DetectProviderAsync(
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var directory = workingDirectory ?? Directory.GetCurrentDirectory();
        _logger.LogDebug("Detecting infrastructure provider in: {Directory}", directory);

        foreach (var provider in _providers)
        {
            _logger.LogDebug("Checking provider: {Provider}", provider.Name);

            if (await provider.CanHandleAsync(directory, cancellationToken))
            {
                _logger.LogInformation("Detected infrastructure provider: {Provider}", provider.DisplayName);
                return provider;
            }
        }

        _logger.LogWarning("No infrastructure provider detected in {Directory}", directory);
        return null;
    }

    /// <summary>
    /// Gets provider by name (for CLI --provider flag)
    /// </summary>
    public IInfrastructureProvider? GetProviderByName(string providerName)
    {
        return _providers.FirstOrDefault(p =>
            p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Lists all available providers
    /// </summary>
    public IEnumerable<IInfrastructureProvider> GetAllProviders()
    {
        return _providers;
    }
}

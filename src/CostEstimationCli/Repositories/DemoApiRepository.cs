using CostEstimationCli.Configuration;
using CostEstimationCli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CostEstimationCli.Repositories;

/// <summary>
/// Demo API repository that returns a local mock response without calling the real API.
/// Used when demo mode is enabled and no API endpoint is available.
/// </summary>
public class DemoApiRepository : IApiRepository
{
    private readonly PulumiSettings _settings;
    private readonly ILogger<DemoApiRepository> _logger;

    private const string DemoResponseFileName = "demo-response.json";

    public DemoApiRepository(
        IOptions<PulumiSettings> settings,
        ILogger<DemoApiRepository> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CostEstimateResponseModel> GetCostEstimateAsync(
        string pulumiPreviewJson,
        CancellationToken cancellationToken = default)
    {
        var responsePath = ResolveDemoResponsePath();

        _logger.LogDebug("DEMO MODE: Loading mock API response from {Path}", responsePath);

        if (!File.Exists(responsePath))
        {
            throw new FileNotFoundException(
                $"Demo response file not found: {responsePath}. " +
                "Ensure 'demo-response.json' exists in the same directory as the demo data file.");
        }

        var json = await File.ReadAllTextAsync(responsePath, cancellationToken);
        var response = JsonConvert.DeserializeObject<CostEstimateResponseModel>(json);

        if (response == null)
        {
            throw new InvalidOperationException($"Failed to deserialize demo response from {responsePath}");
        }

        return response;
    }

    /// <inheritdoc />
    public Task<LicenseResponseModel> ValidateLicenseAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("DEMO MODE: Skipping license validation");

        return Task.FromResult(new LicenseResponseModel
        {
            IsAuthorized = true,
            CustomerName = "Demo User"
        });
    }

    private string ResolveDemoResponsePath()
    {
        var basePath = AppContext.BaseDirectory;
        var dataFilePath = Path.IsPathRooted(_settings.DemoDataPath)
            ? _settings.DemoDataPath
            : Path.Combine(basePath, _settings.DemoDataPath);

        return Path.Combine(Path.GetDirectoryName(dataFilePath)!, DemoResponseFileName);
    }
}

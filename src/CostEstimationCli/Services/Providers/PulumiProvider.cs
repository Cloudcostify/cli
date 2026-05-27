using System.Text.Json;
using CostEstimationCli.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulumi.Automation;

namespace CostEstimationCli.Services.Providers;

/// <summary>
/// Pulumi infrastructure provider
/// </summary>
public class PulumiProvider : IInfrastructureProvider
{
    private readonly PulumiSettings _settings;
    private readonly ILogger<PulumiProvider> _logger;

    public string Name => "Pulumi";
    public string DisplayName => "🟣 Pulumi";

    public PulumiProvider(
        IOptions<PulumiSettings> settings,
        ILogger<PulumiProvider> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CanHandleAsync(string workingDirectory, CancellationToken cancellationToken = default)
    {
        // Check for Pulumi.yaml file
        var pulumiYaml = Path.Combine(workingDirectory, "Pulumi.yaml");
        return await Task.FromResult(File.Exists(pulumiYaml));
    }

    public async Task<string> ExtractResourceJsonAsync(CancellationToken cancellationToken = default)
    {
        // Demo mode: Load from sample file
        if (_settings.DemoMode)
        {
            return await LoadDemoDataAsync(cancellationToken);
        }

        // Production: Export from Pulumi stack
        ValidateSettings();

        try
        {
            _logger.LogDebug("Exporting Pulumi stack: {StackName} for project: {ProjectName}",
                _settings.StackName, _settings.ProjectName);

            var program = PulumiFn.Create(() => { });
            var stackArgs = new InlineProgramArgs(_settings.ProjectName, _settings.StackName, program);

            var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs, cancellationToken);
            var export = await stack.ExportStackAsync(cancellationToken);

            _logger.LogDebug("Pulumi stack exported successfully");
            return JsonSerializer.Serialize(export.Json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Pulumi stack");
            throw;
        }
    }

    private async Task<string> LoadDemoDataAsync(CancellationToken cancellationToken)
    {
        var basePath = AppContext.BaseDirectory;
        var fullPath = Path.IsPathRooted(_settings.DemoDataPath)
            ? _settings.DemoDataPath
            : Path.Combine(basePath, _settings.DemoDataPath);

        _logger.LogDebug("DEMO MODE: Loading Pulumi data from {DemoDataPath}", fullPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Demo data file not found: {fullPath}");
        }

        var jsonContent = await File.ReadAllTextAsync(fullPath, cancellationToken);
        var jsonDocument = JsonDocument.Parse(jsonContent);

        _logger.LogDebug("Demo data loaded successfully");
        return JsonSerializer.Serialize(jsonDocument.RootElement);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.ProjectName))
        {
            throw new InvalidOperationException("Pulumi project name is not configured");
        }

        if (string.IsNullOrWhiteSpace(_settings.StackName))
        {
            throw new InvalidOperationException("Pulumi stack name is not configured");
        }
    }
}

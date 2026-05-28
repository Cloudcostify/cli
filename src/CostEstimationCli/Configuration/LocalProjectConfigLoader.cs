using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CostEstimationCli.Configuration;

/// <summary>
/// Loads the optional <c>cloudcostify-config.json</c> in-repository configuration
/// file from the current working directory (or a specified directory).
/// Returns <see langword="null"/> when the file does not exist.
/// </summary>
public class LocalProjectConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<LocalProjectConfigLoader> _logger;

    public LocalProjectConfigLoader(ILogger<LocalProjectConfigLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to read and deserialise <c>cloudcostify-config.json</c> from
    /// <paramref name="directory"/> (or the process working directory when null).
    /// </summary>
    public async Task<LocalProjectConfig?> LoadAsync(
        string? directory = null,
        CancellationToken cancellationToken = default)
    {
        var searchDir = directory ?? Directory.GetCurrentDirectory();
        var configPath = Path.Combine(searchDir, LocalProjectConfig.FileName);

        if (!File.Exists(configPath))
        {
            _logger.LogDebug("No local project config found at {Path}; using defaults.", configPath);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath, cancellationToken);
            var config = JsonSerializer.Deserialize<LocalProjectConfig>(json, JsonOptions);

            if (config is null)
            {
                _logger.LogWarning(
                    "Local project config at {Path} deserialised to null; ignoring.", configPath);
                return null;
            }

            _logger.LogInformation(
                "Loaded local project config (v{Version}) from {Path}.",
                config.Version, configPath);

            return config;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Local project config at {Path} contains invalid JSON; ignoring.", configPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to read local project config at {Path}; ignoring.", configPath);
            return null;
        }
    }
}

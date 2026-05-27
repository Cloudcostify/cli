using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CostEstimationCli.Services.Providers;

/// <summary>
/// Azure Bicep infrastructure provider
/// Runs 'az bicep build' to generate ARM template JSON
/// </summary>
public class BicepProvider : IInfrastructureProvider
{
    private readonly ILogger<BicepProvider> _logger;
    private readonly string _bicepFile;

    public string Name => "Bicep";
    public string DisplayName => "💪 Azure Bicep";

    public BicepProvider(ILogger<BicepProvider> logger, string? bicepFile = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bicepFile = bicepFile ?? "main.bicep";
    }

    public async Task<bool> CanHandleAsync(string workingDirectory, CancellationToken cancellationToken = default)
    {
        // Check for main.bicep or any .bicep file
        var bicepPath = Path.Combine(workingDirectory, _bicepFile);
        if (File.Exists(bicepPath))
        {
            return true;
        }

        // Check for any .bicep files
        var bicepFiles = Directory.GetFiles(workingDirectory, "*.bicep");
        return await Task.FromResult(bicepFiles.Length > 0);
    }

    public async Task<string> ExtractResourceJsonAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Building Bicep template: {BicepFile}", _bicepFile);

            // Check if Azure CLI is installed
            if (!await IsAzCliInstalledAsync())
            {
                throw new InvalidOperationException(
                    "Azure CLI is not installed or not in PATH. Please install: https://docs.microsoft.com/cli/azure/install-azure-cli");
            }

            // Run az bicep build
            var outputFile = Path.ChangeExtension(_bicepFile, ".json");
            var buildResult = await RunAzBicepBuildAsync(_bicepFile, cancellationToken);

            if (!buildResult.Success)
            {
                throw new InvalidOperationException($"Bicep build failed: {buildResult.Error}");
            }

            // Read the generated ARM template
            if (!File.Exists(outputFile))
            {
                throw new FileNotFoundException($"ARM template not found after build: {outputFile}");
            }

            var armJson = await File.ReadAllTextAsync(outputFile, cancellationToken);
            _logger.LogDebug("ARM template generated successfully: {Size} bytes", armJson.Length);

            return armJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build Bicep template");
            throw;
        }
    }

    private async Task<bool> IsAzCliInstalledAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "az",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<(bool Success, string Output, string Error)> RunAzBicepBuildAsync(
        string bicepFile,
        CancellationToken cancellationToken)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "az",
                Arguments = $"bicep build --file {bicepFile}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null) output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode == 0, output.ToString(), error.ToString());
    }
}

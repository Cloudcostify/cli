using Microsoft.Extensions.Logging;

namespace CostEstimationCli.Services.Providers;

/// <summary>
/// AWS CDK infrastructure provider
/// Reads CloudFormation templates from cdk.out directory
/// </summary>
public class CdkProvider : IInfrastructureProvider
{
    private readonly ILogger<CdkProvider> _logger;
    private readonly string _cdkOutDirectory;

    public string Name => "CDK";
    public string DisplayName => "☁️ AWS CDK";

    public CdkProvider(ILogger<CdkProvider> logger, string? cdkOutDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cdkOutDirectory = cdkOutDirectory ?? "cdk.out";
    }

    public async Task<bool> CanHandleAsync(string workingDirectory, CancellationToken cancellationToken = default)
    {
        // Check for cdk.out directory
        var cdkOutPath = Path.Combine(workingDirectory, _cdkOutDirectory);
        if (Directory.Exists(cdkOutPath))
        {
            return true;
        }

        // Check for cdk.json file
        var cdkJsonPath = Path.Combine(workingDirectory, "cdk.json");
        return await Task.FromResult(File.Exists(cdkJsonPath));
    }

    public async Task<string> ExtractResourceJsonAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Reading CloudFormation template from CDK output: {CdkOut}", _cdkOutDirectory);

            if (!Directory.Exists(_cdkOutDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"CDK output directory not found: {_cdkOutDirectory}. " +
                    $"Please run 'cdk synth' first to generate CloudFormation templates.");
            }

            // Find CloudFormation template files
            var templateFiles = FindCloudFormationTemplates(_cdkOutDirectory);

            if (templateFiles.Length == 0)
            {
                throw new FileNotFoundException(
                    $"No CloudFormation templates found in {_cdkOutDirectory}. " +
                    $"Expected files: *.template.json or *.template.yaml");
            }

            // Use the first template or combine multiple if needed
            var templateFile = templateFiles[0];
            _logger.LogDebug("Using CloudFormation template: {Template}", Path.GetFileName(templateFile));

            var cloudFormationJson = await File.ReadAllTextAsync(templateFile, cancellationToken);

            // If YAML, we'd need to convert to JSON (simplified for now - assumes JSON)
            if (templateFile.EndsWith(".yaml") || templateFile.EndsWith(".yml"))
            {
                throw new NotSupportedException(
                    "YAML CloudFormation templates are not yet supported. " +
                    "Please ensure CDK outputs JSON templates.");
            }

            _logger.LogDebug("CloudFormation template loaded successfully: {Size} bytes", cloudFormationJson.Length);

            return cloudFormationJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read CDK CloudFormation template");
            throw;
        }
    }

    private string[] FindCloudFormationTemplates(string directory)
    {
        var jsonTemplates = Directory.GetFiles(directory, "*.template.json", SearchOption.TopDirectoryOnly);
        var yamlTemplates = Directory.GetFiles(directory, "*.template.yaml", SearchOption.TopDirectoryOnly);

        return jsonTemplates.Concat(yamlTemplates).ToArray();
    }
}

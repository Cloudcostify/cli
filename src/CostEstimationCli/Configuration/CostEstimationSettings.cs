namespace CostEstimationCli.Configuration;

/// <summary>
/// Root configuration settings for the Cost Estimation CLI
/// </summary>
public class CostEstimationSettings
{
    public const string SectionName = "CostEstimation";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public AuthenticationSettings Authentication { get; set; } = new();
}

/// <summary>
/// Authentication configuration settings
/// </summary>
public class AuthenticationSettings
{
    public bool Enabled { get; set; }
}

/// <summary>
/// Pulumi configuration settings
/// </summary>
public class PulumiSettings
{
    public const string SectionName = "Pulumi";

    public string ProjectDirectoryPath { get; set; } = string.Empty;
    public string StackName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public bool DemoMode { get; set; } = false;
    public string DemoDataPath { get; set; } = "samples/pulumi-preview.json";
}

/// <summary>
/// Environment variable names used by the CLI
/// </summary>
public static class EnvironmentVariables
{
    // Azure DevOps detection
    public const string TF_BUILD = "TF_BUILD";

    // Cloudcostify settings
    public const string CLOUDCOSTIFY_BASE_URL = "CLOUDCOSTIFY_BASE_URL";
    public const string CLOUDCOSTIFY_API_KEY = "CLOUDCOSTIFY_API_KEY";
    public const string CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME = "CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME";
    public const string CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH = "CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH";

    // Pulumi settings
    public const string PULUMI_ACCESS_TOKEN = "PULUMI_ACCESS_TOKEN";
    public const string PULUMI_PROJECT_NAME = "PulumiProjectName";

    // GitHub Actions integration
    public const string GITHUB_ACTIONS     = "GITHUB_ACTIONS";
    public const string GITHUB_ENV         = "GITHUB_ENV";
    public const string GITHUB_OUTPUT      = "GITHUB_OUTPUT";
    public const string GITHUB_EVENT_NAME  = "GITHUB_EVENT_NAME";
}

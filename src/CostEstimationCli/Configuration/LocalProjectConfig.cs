namespace CostEstimationCli.Configuration;

/// <summary>
/// In-repository configuration file schema for the Cost Estimation CLI.
/// Loaded from <c>.saasfactory-cost.json</c> in the current working directory.
/// CLI flags take precedence over values in this file.
/// </summary>
public class LocalProjectConfig
{
    public const string FileName = "cloudcostify-config.json";

    /// <summary>Config schema version (currently "1.0").</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Budget settings that override the CLI defaults.</summary>
    public LocalBudgetConfig? Budget { get; set; }

    /// <summary>Resource filtering rules applied before the API payload is sent.</summary>
    public LocalIgnoreConfig? Ignore { get; set; }
}

/// <summary>Budget section of <see cref="LocalProjectConfig"/>.</summary>
public class LocalBudgetConfig
{
    /// <summary>Maximum acceptable monthly cost in <see cref="Currency"/>.</summary>
    public decimal MonthlyLimit { get; set; }

    /// <summary>ISO-4217 currency code (default: "USD").</summary>
    public string Currency { get; set; } = "USD";
}

/// <summary>Ignore-list section of <see cref="LocalProjectConfig"/>.</summary>
public class LocalIgnoreConfig
{
    /// <summary>
    /// Fully-qualified Pulumi resource types to exclude from cost estimation,
    /// e.g. <c>azure-native:resources:ResourceGroup</c>.
    /// </summary>
    public List<string> ResourceTypes { get; set; } = new();

    /// <summary>
    /// Logical resource names (the last segment of the Pulumi URN) to exclude.
    /// </summary>
    public List<string> ResourceNames { get; set; } = new();
}

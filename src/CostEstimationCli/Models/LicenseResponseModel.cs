namespace CostEstimationCli.Models;

/// <summary>
/// Response model for license validation
/// </summary>
public class LicenseResponseModel
{
    public string CustomerName { get; set; } = string.Empty;
    public bool IsAuthorized { get; set; }
    public DateTime Timestamp { get; set; }
}

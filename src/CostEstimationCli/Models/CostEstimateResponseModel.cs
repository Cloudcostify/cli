namespace CostEstimationCli.Models;

/// <summary>
/// Represents aggregate cost calculations across different time periods
/// </summary>
public class AggregateCost
{
    public decimal PerMilliSecond { get; set; }
    public decimal PerSecond { get; set; }
    public decimal PerMinute { get; set; }
    public decimal PerHour { get; set; }
    public decimal PerDay { get; set; }
    public decimal PerWeek { get; set; }
    public decimal PerMonth { get; set; }
    public decimal PerQuarter { get; set; }
    public decimal PerHalfYear { get; set; }
    public decimal PerYear { get; set; }
}

/// <summary>
/// Represents an Azure Kubernetes Service (AKS) managed cluster
/// </summary>
public class AksManagedCluster
{
    public string name { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty;
    public List<NodePool> nodePools { get; set; } = new();
}

/// <summary>
/// Represents cloud resources with cost estimations
/// </summary>
public class CloudResource
{
    public List<AksManagedCluster> aksManagedClusters { get; set; } = new();
}

/// <summary>
/// Represents a node pool in an AKS cluster
/// </summary>
public class NodePool
{
    public string name { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public int nodeCount { get; set; }
    public string os { get; set; } = string.Empty;
    public VirtualMachineSku virtualMachineSku { get; set; } = new();
}

/// <summary>
/// Response model containing cost estimation results from the API
/// </summary>
public class CostEstimateResponseModel
{
    public string currency { get; set; } = string.Empty;
    public string cloudProvider { get; set; } = string.Empty;
    public AggregateCost aggregateCosts { get; set; } = new();
    public List<CloudResource> cloudResources { get; set; } = new();
}

/// <summary>
/// Represents a virtual machine SKU with pricing information
/// </summary>
public class VirtualMachineSku
{
    public string sku { get; set; } = string.Empty;
    public decimal price { get; set; }
    public string priceUnit { get; set; } = string.Empty;
    public string os { get; set; } = string.Empty;
}

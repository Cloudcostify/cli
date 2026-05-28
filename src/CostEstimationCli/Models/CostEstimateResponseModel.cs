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
    public string resourceGroupName { get; set; } = string.Empty;
    public string tier { get; set; } = "Free";
    public List<NodePool> nodePools { get; set; } = new();
    public AggregateCost controlPlaneCosts { get; set; } = new();
    public AggregateCost aggregateAKSClusterCosts { get; set; } = new();
}

/// <summary>
/// Represents cloud resources with cost estimations
/// </summary>
public class CloudResource
{
    public List<AksManagedCluster> aksManagedClusters { get; set; } = new();
    public List<VirtualMachine> virtualMachines { get; set; } = new();
    public List<VirtualMachineScaleSet> virtualMachineScaleSets { get; set; } = new();
    public List<SqlDatabase> sqlDatabases { get; set; } = new();
    public List<SqlManagedInstance> sqlManagedInstances { get; set; } = new();
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
    public AggregateCost aggregateNodePoolCosts { get; set; } = new();
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
    public List<UnsupportedResource>? unsupportedResources { get; set; }
}

/// <summary>
/// Represents a resource that could not be cost estimated due to an unsupported resource type
/// </summary>
public class UnsupportedResource
{
    public string resourceName { get; set; } = string.Empty;
    public string resourceType { get; set; } = string.Empty;
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

/// <summary>
/// Represents an Azure Virtual Machine with cost estimation
/// </summary>
public class VirtualMachine
{
    public string name { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty;
    public string resourceGroupName { get; set; } = string.Empty;
    public string operatingSystem { get; set; } = string.Empty;
    public string priority { get; set; } = string.Empty;
    public VirtualMachineSku virtualMachineSku { get; set; } = new();
    public AggregateCost aggregateVirtualMachineCosts { get; set; } = new();
}

/// <summary>
/// Represents an Azure Virtual Machine Scale Set with cost estimation
/// </summary>
public class VirtualMachineScaleSet
{
    public string name { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty;
    public string resourceGroupName { get; set; } = string.Empty;
    public int instanceCount { get; set; }
    public string operatingSystem { get; set; } = string.Empty;
    public string priority { get; set; } = string.Empty;
    public VirtualMachineSku virtualMachineSku { get; set; } = new();
    public AggregateCost aggregateVirtualMachineScaleSetCosts { get; set; } = new();
}

/// <summary>
/// Represents an Azure SQL Database SKU with pricing information
/// </summary>
public class SqlDatabaseSku
{
    public string sku { get; set; } = string.Empty;
    public string tier { get; set; } = string.Empty;
    public int capacity { get; set; }
    public decimal price { get; set; }
    public string priceUnit { get; set; } = string.Empty;
}

/// <summary>
/// Represents an Azure SQL Database with cost estimation
/// </summary>
public class SqlDatabase
{
    public string name { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty;
    public string resourceGroupName { get; set; } = string.Empty;
    public string serverName { get; set; } = string.Empty;
    public SqlDatabaseSku sqlDatabaseSku { get; set; } = new();
    public AggregateCost aggregateSqlDatabaseCosts { get; set; } = new();
}

/// <summary>
/// Represents an Azure SQL Managed Instance SKU
/// </summary>
public class SqlManagedInstanceSku
{
    public string sku { get; set; } = string.Empty;
    public string tier { get; set; } = string.Empty;
    public string family { get; set; } = string.Empty;
    public int vCores { get; set; }
}

/// <summary>
/// Represents pricing breakdown for an Azure SQL Managed Instance
/// </summary>
public class SqlManagedInstancePricing
{
    public decimal computePrice { get; set; }
    public string computePriceUnit { get; set; } = string.Empty;
    public decimal sqlLicensePrice { get; set; }
    public string sqlLicensePriceUnit { get; set; } = string.Empty;
    public decimal storagePrice { get; set; }
    public string storagePriceUnit { get; set; } = string.Empty;
}

/// <summary>
/// Represents an Azure SQL Managed Instance with cost estimation
/// </summary>
public class SqlManagedInstance
{
    public string name { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty;
    public string resourceGroupName { get; set; } = string.Empty;
    public string licenseType { get; set; } = string.Empty;
    public int storageSizeInGB { get; set; }
    public SqlManagedInstanceSku sqlManagedInstanceSku { get; set; } = new();
    public SqlManagedInstancePricing pricing { get; set; } = new();
    public AggregateCost aggregateSqlManagedInstanceCosts { get; set; } = new();
}

namespace CostEstimationCli.Models;

/// <summary>
/// Represents the cost change derived from Pulumi preview step operations,
/// distinguishing between resources being added, removed, or updated.
/// </summary>
public class CostDelta
{
    /// <summary>Estimated monthly cost of resources being created (added spend).</summary>
    public decimal AddedMonthly { get; init; }

    /// <summary>
    /// Estimated monthly cost of resources being deleted.
    /// This is 0 when the pre-existing cost cannot be determined from the preview alone.
    /// </summary>
    public decimal SavedMonthly { get; init; }

    /// <summary>Net monthly cost change: AddedMonthly - SavedMonthly.</summary>
    public decimal NetMonthly => AddedMonthly - SavedMonthly;

    /// <summary>Number of resources being created.</summary>
    public int CreatedCount { get; init; }

    /// <summary>Number of resources being deleted.</summary>
    public int DeletedCount { get; init; }

    /// <summary>Number of resources being updated (configuration change).</summary>
    public int UpdatedCount { get; init; }

    /// <summary>Number of resources that remain unchanged.</summary>
    public int UnchangedCount { get; init; }
}

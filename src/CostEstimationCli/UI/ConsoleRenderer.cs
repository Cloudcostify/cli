using CostEstimationCli.Configuration;
using CostEstimationCli.Models;
using CostEstimationCli.Services.Providers;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using System.Globalization;

namespace CostEstimationCli.UI;

public static class ConsoleRenderer
{
    // ── Palette ────────────────────────────────────────────────────────────────
    private static readonly CultureInfo UsCulture = new("en-US");
    private const decimal DefaultBudgetLimit = 500.00m;

    // Premium Cyberpunk Sunset-gradient og kontraster
    private const string CyberOrange    = "#F26522";
    private const string GradientAccent  = "#FF2A85"; // Cyber Pink/Magenta der fletter perfekt ind i orange
    private const string NeonCyan        = "#00e5ff";
    private const string DarkGrey        = "grey30";
    private const string LightGrey       = "grey62";

    // Dynamisk tjek af terminalens evner (CI/CD vs lokal moderne terminal)
    private static bool SupportsUnicode => AnsiConsole.Profile.Capabilities.Unicode;

    // ── BLOK-BANNER (Ultra-fed og lige version til Cloudcostify) ────────────────
    private static readonly string[] BannerLines =
    [
        @"██████╗██╗      ██████╗ ██╗   ██╗██████╗  ██████╗ ██████╗ ███████╗████████╗██╗███████╗██╗   ██╗",
        @"██╔════╝██║     ██╔═══██╗██║   ██║██╔══██╗██╔════╝██╔═══██╗██╔════╝╚══██╔══╝██║██╔════╝╚██╗ ██╔╝",
        @"██║     ██║     ██║   ██║██║   ██║██║  ██║██║     ██║   ██║███████╗   ██║   ██║█████╗   ╚████╔╝ ",
        @"██║     ██║     ██║   ██║██║   ██║██║  ██║██║     ██║   ██║╚════██║   ██║   ██║██╔══╝    ╚██╔╝  ",
        @"╚██████╗███████╗╚██████╔╝╚██████╔╝██████╔╝╚██████╗╚██████╔╝███████║   ██║   ██║██║        ██║   ",
        @" ╚═════╝╚══════╝ ╚═════╝  ╚═════╝ ╚═════╝  ╚═════╝ ╚═════╝ ╚══════╝   ╚═╝   ╚═╝╚═╝        ╚═╝  "
    ];



    // ── Header & Branding ──────────────────────────────────────────────────────

    public static void RenderHeader(IConfiguration configuration, IInfrastructureProvider provider)
    {
        AnsiConsole.Clear();
        RenderBanner();

        var arrowIcon = SupportsUnicode ? "▲" : ">";
        AnsiConsole.MarkupLine($" [bold {CyberOrange}]{arrowIcon}[/] [#ffffff]Universal cost estimation for Infrastructure as Code[/]");
        AnsiConsole.WriteLine();

        var pulumiSettings = configuration.GetSection(PulumiSettings.SectionName);
        var isDemoMode = string.Equals(pulumiSettings["DemoMode"], "true", StringComparison.OrdinalIgnoreCase);

        if (isDemoMode)
            RenderDemoModePanel(pulumiSettings);
    }

    private static void RenderBanner()
    {
        // Genererer en flydende lodret gradient på tværs af ASCII-logoet
        for (int i = 0; i < BannerLines.Length; i++)
        {
            var color = i < 2 ? CyberOrange : i < 4 ? "#FA4655" : GradientAccent;
            AnsiConsole.MarkupLine($"[bold {color}]{BannerLines[i]}[/]");
        }
    }

    private static void RenderDemoModePanel(IConfigurationSection settings)
    {
        var dataPath = settings["DemoDataPath"] ?? "samples/pulumi-preview.json";
        var warnIcon = SupportsUnicode ? "⚠️" : "[!]";

        var content = new Markup(
            $"{warnIcon}  Using sample data from: [{NeonCyan}]{Markup.Escape(dataPath)}[/]\n" +
            $"   No cloud or Pulumi account required in demo mode.");

        AnsiConsole.Write(
            new Panel(content)
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse(DarkGrey))
                .Header($"[bold #ffd700] DEMO MODE ENABLED [/]", Justify.Left)
                .Padding(2, 0));

        AnsiConsole.WriteLine();
    }

    // ── Live Status Spinner ────────────────────────────────────────────────────

    public static async Task<T> ExecuteWithStatusAsync<T>(
        string initialMessage,
        Func<Action<string>, Task<T>> work)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse($"{CyberOrange} bold"))
            .StartAsync($"[{LightGrey}]{Markup.Escape(initialMessage)}[/]", async ctx =>
            {
                void UpdateStatus(string msg) => ctx.Status($"[{LightGrey}]{Markup.Escape(msg)}[/]");
                var result = await work(UpdateStatus);
                return result;
            });
    }

    public static void LogStep(string icon, string message, string? detail = null)
    {
        AnsiConsole.MarkupLine(
            $" [{CyberOrange}]{Markup.Escape(icon)}[/] [{LightGrey}]{Markup.Escape(message)}[/]" +
            (detail is not null ? $" [bold {DarkGrey}][[{Markup.Escape(detail)}]][/]" : ""));
    }

    // ── Results & HTML Recording ───────────────────────────────────────────────

    public static void RenderResults(CostEstimateResponseModel estimate)
    {
        // Begynd at optage terminal-outputtet, så det kan gemmes som HTML til CI/CD pull requests
        AnsiConsole.Record();

        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Rule($"[bold {LightGrey}]COST SUMMARY[/]")
                .RuleStyle(DarkGrey)
                .LeftJustified());

        AnsiConsole.WriteLine();
        RenderCostSummary(estimate.aggregateCosts);

        if (estimate.cloudResources?.Any() == true)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(
                new Rule($"[bold {LightGrey}]RESOURCE DETAILS[/]")
                    .RuleStyle(DarkGrey)
                    .LeftJustified());

            AnsiConsole.WriteLine();
            RenderResourceHierarchy(estimate);
        }

        RenderProviderFooter();
    }

    // Kald denne efter RenderResults i din pipeline, hvis du vil dumpe en HTML-rapport til CI/CD artifacting
    public static void SaveHtmlReport(string filePath)
    {
        if (AnsiConsole.Profile.Capabilities.Ansi)
        {
            var html = AnsiConsole.ExportHtml();
            File.WriteAllText(filePath, html);
        }
    }

    // ── Cost Summary & Delta Architecture ──────────────────────────────────────

    private static void RenderCostSummary(AggregateCost costs)
    {
        var table = new Table()
            .Border(TableBorder.MinimalHeavyHead)
            .BorderColor(Color.Grey30);

        table.AddColumn("Period");
        table.AddColumn(new TableColumn("Amount").RightAligned());
        table.AddColumn(new TableColumn("Delta (Diff)").RightAligned()); // Ny Diff-kolonne til Pulumi Previews
        table.AddColumn("Status");

        // Her kan du binde dine reelle beregnede ændringer (f.eks. fra Pulumi diffs)
        // Vi sender nogle mock-tal med her for at illustrere det visuelle udtryk
        AddCostRow(table, "Per Hour", costs.PerHour, 0.12m, false);
        AddCostRow(table, "Per Day", costs.PerDay, 2.88m, false);
        AddCostRow(table, "Per Week", costs.PerWeek, 20.16m, false);
        AddCostRow(table, "Per Month", costs.PerMonth, 86.40m, true);
        AddCostRow(table, "Per Year", costs.PerYear, 1036.80m, false);

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        RenderBudgetGuard(costs.PerMonth);
    }

    private static void AddCostRow(Table table, string label, decimal amount, decimal? delta, bool isTarget)
    {
        var formattedAmount = FormatCurrencyPlain(amount);
        string deltaMarkup = "";

        // Intelligent prissætnings-indikation (Grøn for stigning/ændring, rød for fald)
        if (delta.HasValue && delta.Value != 0)
        {
            var sign = delta.Value > 0 ? "+" : "";
            var color = delta.Value > 0 ? CyberOrange : "green";
            deltaMarkup = $"[{color}]{sign}{FormatCurrencyPlain(delta.Value)}[/]";
        }

        if (isTarget)
        {
            table.AddRow(
                $"[bold {GradientAccent}]{label}[/]",
                $"[bold {GradientAccent}]{formattedAmount}[/]",
                $"[bold {GradientAccent}]{deltaMarkup}[/]",
                $"[bold {GradientAccent}]◀ Target[/]"
            );
        }
        else
        {
            table.AddRow(
                $"[{LightGrey}]{label}[/]",
                $"[{NeonCyan}]{formattedAmount}[/]",
                deltaMarkup,
                ""
            );
        }
    }

    // ── Resource Hierarchy Tree & Hyperlinks ───────────────────────────────────

    private static void RenderResourceHierarchy(CostEstimateResponseModel estimate)
    {
        foreach (var resource in estimate.cloudResources ?? [])
        {
            foreach (var cluster in resource.aksManagedClusters ?? [])
            {
                RenderClusterTree(estimate.cloudProvider, cluster);
                AnsiConsole.WriteLine();
            }
        }
    }

    private static void RenderClusterTree(string cloudProvider, AksManagedCluster cluster)
    {
        var providerColor = GetProviderColor(cloudProvider);
        var providerIcon = SupportsUnicode ? "▰" : "■";
        var clusterIcon = SupportsUnicode ? "🖥️ " : "";

        var tree = new Tree($"[bold {providerColor}]{providerIcon} {Markup.Escape(cloudProvider)}[/] [{LightGrey}]({Markup.Escape(cluster.location)})[/]")
            .Style(new Style(foreground: Color.Grey30));

        // NATIVE HYPERLINK: Gør klyngen klikbar direkte til Azure Portalen i understøttede terminaler
        var portalUrl = $"https://portal.azure.com/#panelId/resource/search?q={Uri.EscapeDataString(cluster.name)}";
        var clusterNode = tree.AddNode(
            $"{clusterIcon}[white]AKS Managed Cluster:[/] [link={portalUrl}][bold {NeonCyan}]{Markup.Escape(cluster.name)}[/][/]");

        foreach (var pool in cluster.nodePools ?? [])
            RenderNodePoolBranch(clusterNode, pool);

        AnsiConsole.Write(tree);
    }

    private static void RenderNodePoolBranch(TreeNode parent, NodePool pool)
    {
        var poolIcon = SupportsUnicode ? "📦 " : "";
        var poolNode = parent.AddNode(
            $"{poolIcon}[white]Node Pool:[/] [{NeonCyan}]{Markup.Escape(pool.name)}[/] [{DarkGrey}]({Markup.Escape(pool.os)})[/]");

        poolNode.AddNode($"[{LightGrey}]Count:[/]      [{CyberOrange}]{pool.nodeCount}[/] [{LightGrey}]nodes[/]");
        
        // SKU HYPERLINK: Klik direkte videre til Azure SKU dokumentation/priser
        var skuUrl = $"https://azure.microsoft.com/en-us/pricing/details/virtual-machines/series/";
        poolNode.AddNode($"[{LightGrey}]VM SKU:[/]     [link={skuUrl}][{NeonCyan}]{Markup.Escape(pool.virtualMachineSku.sku)}[/][/]");

        var subtotal = pool.nodeCount * pool.virtualMachineSku.price;
        poolNode.AddNode(
            $"[{LightGrey}]Base Price:[/] [{NeonCyan}]{FormatCurrencyPlain(pool.virtualMachineSku.price)}[/] [{DarkGrey}]/{Markup.Escape(pool.virtualMachineSku.priceUnit)}[/] " +
            $"[{DarkGrey}]([/][{CyberOrange}]{FormatCurrencyPlain(subtotal)}[/][{DarkGrey}]/mo total)[/]");
    }

    // ── Budget Guard ───────────────────────────────────────────────────────────

    private static void RenderBudgetGuard(decimal monthlyCost)
    {
        var pct = Math.Min((double)(monthlyCost / DefaultBudgetLimit * 100.0m), 100.0);

        var (color, icon) = pct switch
        {
            < 50  => (CyberOrange, SupportsUnicode ? "✔" : "[OK]"),
            < 80  => ("#ffd700",   SupportsUnicode ? "⚠" : "[!]"),
            _     => ("#ff5555",   SupportsUnicode ? "✗" : "[X]"),
        };

        const int barWidth = 20;
        var filledCount = (int)Math.Round(pct / 100.0 * barWidth);
        var emptyCount  = barWidth - filledCount;

        // Vercel-style progress bar der falder tilbage til '#' hvis Unicode mangler
        char blockChar = SupportsUnicode ? '█' : '#';
        string filledBar = new string(blockChar, filledCount);
        string emptyBar  = new string(blockChar, emptyCount);

        AnsiConsole.MarkupLine($" [{LightGrey}]Budget Guard[/]");
        AnsiConsole.MarkupLine(
            $"   [{color}]{icon}[/] [{color}]{filledBar}[/][{DarkGrey}]{emptyBar}[/]  " +
            $"[{color}]{FormatCurrencyPlain(monthlyCost)}[/] [{LightGrey}]/[/] [{LightGrey}]{FormatCurrencyPlain(DefaultBudgetLimit)} monthly budget ({pct:N0}%)[/]");
    }

    // ── Provider Footer ────────────────────────────────────────────────────────

    private static void RenderProviderFooter()
    {
        AnsiConsole.Write(new Rule().RuleStyle(DarkGrey));

        var check = SupportsUnicode ? "✔" : "X";
        AnsiConsole.MarkupLine(
            $" [{LightGrey}]Supported modules:[/] [{CyberOrange}]AWS {check}[/] [{DarkGrey}]|[/]" +
            $" [{CyberOrange}]Azure {check}[/] [{DarkGrey}]|[/]" +
            $" [{LightGrey}]GCP (Soon) | OCI (Soon)[/]");

        AnsiConsole.WriteLine();
    }

    // ── Error & Help ───────────────────────────────────────────────────────────

    public static void RenderError(string title, string message, string? details = null)
    {
        var errorIcon = SupportsUnicode ? "✗" : "[X]";
        var content = new Markup(
            $"[red]{Markup.Escape(message)}[/]" +
            (details is not null ? $"\n\n[{LightGrey}]{Markup.Escape(details)}[/]" : ""));

        AnsiConsole.Write(
            new Panel(content)
                .Header($"[bold red] {errorIcon} {Markup.Escape(title)} [/]", Justify.Left)
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse("red dim")));
    }

    public static void RenderHelp()
    {
        AnsiConsole.Clear();
        RenderBanner();

        var arrowIcon = SupportsUnicode ? "▲" : ">";
        AnsiConsole.MarkupLine($" [{CyberOrange}]{arrowIcon}[/] [white]Universal cost estimation for Infrastructure as Code[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .NoBorder()
            .HideHeaders()
            .AddColumn(new TableColumn("").Width(34))
            .AddColumn(new TableColumn(""));

        table.AddRow(new Markup($"[bold {NeonCyan}]Usage[/]"), new Markup("[white]cloudcostify[/] [dim][options][/]"));
        table.AddEmptyRow();

        table.AddRow(new Markup($"[bold {LightGrey}]OPTIONS[/]"), new Markup(""));
        table.AddRow(new Markup($"[{NeonCyan}]  --provider[/] [dim]<name>[/]"),             new Markup($"[{LightGrey}]IaC provider:[/]  [white]pulumi[/]  [white]bicep[/]  [white]cdk[/]"));
        table.AddRow(new Markup($"[{NeonCyan}]  --directory[/] [dim]<path>[/]"),            new Markup($"[{LightGrey}]Working directory[/]  [dim](default: current)[/]"));
        table.AddRow(new Markup($"[{NeonCyan}]  --help[/][dim],[/] [{NeonCyan}]-h[/]"),     new Markup($"[{LightGrey}]Show this help message[/]"));
        table.AddEmptyRow();

        table.AddRow(new Markup($"[bold {LightGrey}]EXAMPLES[/]"),                                   new Markup(""));
        table.AddRow(new Markup(""), new Markup($"[white]cloudcostify[/]                       [{DarkGrey}]# Auto-detect provider[/]"));
        table.AddRow(new Markup(""), new Markup($"[white]cloudcostify --provider bicep[/]      [{DarkGrey}]# Azure Bicep[/]"));
        table.AddRow(new Markup(""), new Markup($"[white]cloudcostify --provider cdk[/]        [{DarkGrey}]# AWS CDK[/]"));
        table.AddRow(new Markup(""), new Markup($"[white]cloudcostify --provider pulumi[/]     [{DarkGrey}]# Pulumi[/]"));

        AnsiConsole.Write(
            new Panel(table)
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse(DarkGrey))
                .Padding(2, 1));

        AnsiConsole.WriteLine();
        RenderProviderFooter();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string GetProviderColor(string cloudProvider) =>
        cloudProvider.ToLowerInvariant() switch
        {
            "azure" or "azurerm" => NeonCyan,
            "aws"                => "#FFD700",
            "gcp" or "google"    => CyberOrange,
            _                    => NeonCyan,
        };

    private static string FormatCurrencyPlain(decimal amount) =>
        $"${amount.ToString("N2", UsCulture)}";
}
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

    public static void RenderResults(CostEstimateResponseModel estimate, decimal? budget = null)
    {
        // Begin recording terminal output for HTML CI/CD artefact export
        AnsiConsole.Record();

        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Rule($"[bold {LightGrey}]COST SUMMARY[/]")
                .RuleStyle(DarkGrey)
                .LeftJustified());

        AnsiConsole.WriteLine();
        RenderCostSummary(estimate.aggregateCosts, budget, estimate.Delta);

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

        if (estimate.unsupportedResources?.Any() == true)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(
                new Rule($"[bold #ffd700]UNSUPPORTED RESOURCES[/]")
                    .RuleStyle(DarkGrey)
                    .LeftJustified());

            AnsiConsole.WriteLine();
            RenderUnsupportedResources(estimate.unsupportedResources);
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

    // ── Delta Summary ──────────────────────────────────────────────────────────

    private static void RenderDeltaSummary(CostDelta delta)
    {
        string netColor;
        if (delta.NetMonthly > 0)       netColor = CyberOrange;
        else if (delta.NetMonthly < 0)  netColor = "green";
        else                            netColor = LightGrey;

        string arrowIcon;
        if (SupportsUnicode) arrowIcon = delta.NetMonthly >= 0 ? "▲" : "▼";
        else                 arrowIcon = delta.NetMonthly >= 0 ? "+" : "-";

        var sign         = delta.NetMonthly >= 0 ? "+" : "";
        var deltaIcon    = SupportsUnicode ? "Δ" : "~";

        AnsiConsole.MarkupLine(
            $" [{LightGrey}]{deltaIcon} Net Change[/]   " +
            $"[bold {netColor}]{arrowIcon} {sign}{FormatCurrencyPlain(delta.NetMonthly)} / month[/]");

        // Op breakdown
        var parts = new List<string>();
        if (delta.CreatedCount   > 0) parts.Add($"[{NeonCyan}]{delta.CreatedCount} created[/]");
        if (delta.DeletedCount   > 0) parts.Add($"[green]{delta.DeletedCount} deleted[/]");
        if (delta.UpdatedCount   > 0) parts.Add($"[#ffd700]{delta.UpdatedCount} updated[/]");
        if (delta.UnchangedCount > 0) parts.Add($"[{DarkGrey}]{delta.UnchangedCount} unchanged[/]");

        if (parts.Count > 0)
        {
            AnsiConsole.MarkupLine(
                $"   [{DarkGrey}]({string.Join($"[{DarkGrey}] · [/]", parts)})[/]");
        }
    }

    // ── Resource Hierarchy Tree & Hyperlinks ───────────────────────────────────

    private static void RenderCostSummary(AggregateCost costs, decimal? budget = null, CostDelta? delta = null)
    {
        var table = new Table()
            .Border(TableBorder.MinimalHeavyHead)
            .BorderColor(Color.Grey30);

        table.AddColumn("Period");
        table.AddColumn(new TableColumn("Amount").RightAligned());
        table.AddColumn(new TableColumn("Net Change").RightAligned());
        table.AddColumn("Status");

        // Derive per-period deltas from the monthly net change
        decimal? hourlyDelta  = delta?.NetMonthly is not 0 ? delta!.NetMonthly / 730m  : null;
        decimal? dailyDelta   = delta?.NetMonthly is not 0 ? delta!.NetMonthly / 30m   : null;
        decimal? weeklyDelta  = delta?.NetMonthly is not 0 ? delta!.NetMonthly / 4.33m : null;
        decimal? monthlyDelta = delta?.NetMonthly is not 0 ? delta!.NetMonthly         : null;
        decimal? yearlyDelta  = delta?.NetMonthly is not 0 ? delta!.NetMonthly * 12m   : null;

        AddCostRow(table, "Per Hour",  costs.PerHour,  hourlyDelta,  false);
        AddCostRow(table, "Per Day",   costs.PerDay,   dailyDelta,   false);
        AddCostRow(table, "Per Week",  costs.PerWeek,  weeklyDelta,  false);
        AddCostRow(table, "Per Month", costs.PerMonth, monthlyDelta, true);
        AddCostRow(table, "Per Year",  costs.PerYear,  yearlyDelta,  false);

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        RenderBudgetGuard(costs.PerMonth, budget);

        // ── Delta summary ─────────────────────────────────────────────────────
        if (delta is not null)
        {
            AnsiConsole.WriteLine();
            RenderDeltaSummary(delta);
        }
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
        var cloudProvider = estimate.cloudProvider;
        var providerColor = GetProviderColor(cloudProvider);
        var providerIcon  = SupportsUnicode ? "▰" : "■";
        var rgIcon        = SupportsUnicode ? "📁 " : "";
        var locationIcon  = SupportsUnicode ? "📍 " : "";

        // Group all resources by resource group → location
        var rgOrder   = new List<string>();
        var locOrder  = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var clusterGroups = new Dictionary<(string, string), List<AksManagedCluster>>();
        var vmGroups      = new Dictionary<(string, string), List<VirtualMachine>>();
        var vmssGroups    = new Dictionary<(string, string), List<VirtualMachineScaleSet>>();
        var dbGroups      = new Dictionary<(string, string), List<SqlDatabase>>();
        var miGroups      = new Dictionary<(string, string), List<SqlManagedInstance>>();

        void Track(string rg, string loc)
        {
            if (!locOrder.ContainsKey(rg)) { rgOrder.Add(rg); locOrder[rg] = []; }
            if (!locOrder[rg].Contains(loc, StringComparer.OrdinalIgnoreCase)) locOrder[rg].Add(loc);
        }

        foreach (var resource in estimate.cloudResources ?? [])
        {
            foreach (var item in resource.aksManagedClusters ?? [])
            {
                Track(item.resourceGroupName, item.location);
                var key = (item.resourceGroupName, item.location);
                if (!clusterGroups.ContainsKey(key)) clusterGroups[key] = [];
                clusterGroups[key].Add(item);
            }
            foreach (var item in resource.virtualMachines ?? [])
            {
                Track(item.resourceGroupName, item.location);
                var key = (item.resourceGroupName, item.location);
                if (!vmGroups.ContainsKey(key)) vmGroups[key] = [];
                vmGroups[key].Add(item);
            }
            foreach (var item in resource.virtualMachineScaleSets ?? [])
            {
                Track(item.resourceGroupName, item.location);
                var key = (item.resourceGroupName, item.location);
                if (!vmssGroups.ContainsKey(key)) vmssGroups[key] = [];
                vmssGroups[key].Add(item);
            }
            foreach (var item in resource.sqlDatabases ?? [])
            {
                Track(item.resourceGroupName, item.location);
                var key = (item.resourceGroupName, item.location);
                if (!dbGroups.ContainsKey(key)) dbGroups[key] = [];
                dbGroups[key].Add(item);
            }
            foreach (var item in resource.sqlManagedInstances ?? [])
            {
                Track(item.resourceGroupName, item.location);
                var key = (item.resourceGroupName, item.location);
                if (!miGroups.ContainsKey(key)) miGroups[key] = [];
                miGroups[key].Add(item);
            }
        }

        var tree = new Tree($"[bold {providerColor}]{providerIcon} {Markup.Escape(cloudProvider)}[/]")
            .Style(new Style(foreground: Color.Grey30));

        foreach (var rg in rgOrder)
        {
            var rgNode = tree.AddNode(
                $"{rgIcon}[{LightGrey}]Resource Group:[/] [{NeonCyan}]{Markup.Escape(rg)}[/]");

            foreach (var loc in locOrder[rg])
            {
                var locNode = rgNode.AddNode(
                    $"{locationIcon}[{LightGrey}]{Markup.Escape(loc)}[/]");

                var key = (rg, loc);

                if (clusterGroups.TryGetValue(key, out var clusters))
                    foreach (var c in clusters) AddClusterNode(locNode, c);

                if (vmGroups.TryGetValue(key, out var vms))
                    foreach (var v in vms) AddVirtualMachineNode(locNode, v);

                if (vmssGroups.TryGetValue(key, out var vmssList))
                    foreach (var s in vmssList) AddVirtualMachineScaleSetNode(locNode, s);

                if (dbGroups.TryGetValue(key, out var dbs))
                    foreach (var d in dbs) AddSqlDatabaseNode(locNode, d);

                if (miGroups.TryGetValue(key, out var mis))
                    foreach (var m in mis) AddSqlManagedInstanceNode(locNode, m);
            }
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
    }

    private static void AddClusterNode(TreeNode parent, AksManagedCluster cluster)
    {
        var clusterIcon = SupportsUnicode ? "🖥️ " : "";

        // NATIVE HYPERLINK: Gør klyngen klikbar direkte til Azure Portalen i understøttede terminaler
        var portalUrl = $"https://portal.azure.com/#panelId/resource/search?q={Uri.EscapeDataString(cluster.name)}";
        var clusterNode = parent.AddNode(
            $"{clusterIcon}[white]AKS Managed Cluster:[/] [link={portalUrl}][bold {NeonCyan}]{Markup.Escape(cluster.name)}[/][/]");

        foreach (var pool in cluster.nodePools ?? [])
            RenderNodePoolBranch(clusterNode, pool);

        var tierNormalized = cluster.tier?.Trim() ?? "Free";
        var isStandardOrPremium =
            string.Equals(tierNormalized, "Standard", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tierNormalized, "Premium", StringComparison.OrdinalIgnoreCase);

        var tierColor = isStandardOrPremium ? CyberOrange : DarkGrey;
        clusterNode.AddNode(
            $"[{LightGrey}]Tier:[/]         [{tierColor}]{Markup.Escape(tierNormalized)}[/]");

        if (isStandardOrPremium && cluster.controlPlaneCosts.PerHour > 0)
        {
            var slaIcon = SupportsUnicode ? "🛡️ " : "";
            var slaNode = clusterNode.AddNode(
                $"{slaIcon}[{LightGrey}]Control Plane SLA:[/] [{NeonCyan}]{FormatCurrencyPlain(cluster.controlPlaneCosts.PerHour)}[/] [{DarkGrey}]/hr[/] " +
                $"[{DarkGrey}]([/][{CyberOrange}]{FormatCurrencyPlain(cluster.controlPlaneCosts.PerMonth)}[/][{DarkGrey}]/mo)[/]");
        }

        clusterNode.AddNode(
            $"[{LightGrey}]Cluster Total:[/] [{NeonCyan}]{FormatCurrencyPlain(cluster.aggregateAKSClusterCosts.PerHour)}[/] [{DarkGrey}]/hr[/] " +
            $"[{DarkGrey}]([/][{CyberOrange}]{FormatCurrencyPlain(cluster.aggregateAKSClusterCosts.PerMonth)}[/][{DarkGrey}]/mo)[/]");
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
            $"[{DarkGrey}]([/][{CyberOrange}]{FormatCurrencyPlain(subtotal)}[/][{DarkGrey}]/hr total)[/]");
    }

    private static void AddVirtualMachineNode(TreeNode parent, VirtualMachine vm)
    {
        var vmIcon = SupportsUnicode ? "💻 " : "";

        var portalUrl = $"https://portal.azure.com/#panelId/resource/search?q={Uri.EscapeDataString(vm.name)}";
        var vmNode = parent.AddNode(
            $"{vmIcon}[white]Virtual Machine:[/] [link={portalUrl}][bold {NeonCyan}]{Markup.Escape(vm.name)}[/][/]");

        var skuUrl = "https://azure.microsoft.com/en-us/pricing/details/virtual-machines/series/";
        vmNode.AddNode($"[{LightGrey}]VM SKU:[/]   [link={skuUrl}][{NeonCyan}]{Markup.Escape(vm.virtualMachineSku.sku)}[/][/]");
        vmNode.AddNode($"[{LightGrey}]OS:[/]       [{CyberOrange}]{Markup.Escape(vm.operatingSystem)}[/]");
        vmNode.AddNode($"[{LightGrey}]Priority:[/] [{LightGrey}]{Markup.Escape(vm.priority)}[/]");
        vmNode.AddNode(
            $"[{LightGrey}]Price:[/]    [{NeonCyan}]{FormatCurrencyPlain(vm.virtualMachineSku.price)}[/] [{DarkGrey}]/{Markup.Escape(vm.virtualMachineSku.priceUnit)}[/] " +
            $"[{DarkGrey}]([/][{CyberOrange}]{FormatCurrencyPlain(vm.aggregateVirtualMachineCosts.PerMonth)}[/][{DarkGrey}]/mo)[/]");
    }

    private static void AddVirtualMachineScaleSetNode(TreeNode parent, VirtualMachineScaleSet vmss)
    {
        var vmssIcon = SupportsUnicode ? "🖥️ " : "";

        var portalUrl = $"https://portal.azure.com/#panelId/resource/search?q={Uri.EscapeDataString(vmss.name)}";
        var vmssNode = parent.AddNode(
            $"{vmssIcon}[white]VM Scale Set:[/] [link={portalUrl}][bold {NeonCyan}]{Markup.Escape(vmss.name)}[/][/]");

        var skuUrl = "https://azure.microsoft.com/en-us/pricing/details/virtual-machines/series/";
        vmssNode.AddNode($"[{LightGrey}]VM SKU:[/]     [link={skuUrl}][{NeonCyan}]{Markup.Escape(vmss.virtualMachineSku.sku)}[/][/]");
        vmssNode.AddNode($"[{LightGrey}]Instances:[/]  [{CyberOrange}]{vmss.instanceCount}[/]");
        vmssNode.AddNode($"[{LightGrey}]OS:[/]         [{CyberOrange}]{Markup.Escape(vmss.operatingSystem)}[/]");

        var perInstance = vmss.virtualMachineSku.price;
        var total = perInstance * vmss.instanceCount;
        vmssNode.AddNode(
            $"[{LightGrey}]Base Price:[/] [{NeonCyan}]{FormatCurrencyPlain(perInstance)}[/] [{DarkGrey}]/{Markup.Escape(vmss.virtualMachineSku.priceUnit)}[/] " +
            $"[{DarkGrey}]([/][{CyberOrange}]{FormatCurrencyPlain(vmss.aggregateVirtualMachineScaleSetCosts.PerMonth)}[/][{DarkGrey}]/mo total)[/]");
    }

    private static void AddSqlDatabaseNode(TreeNode parent, SqlDatabase db)
    {
        var dbIcon = SupportsUnicode ? "🗄️ " : "";

        var portalUrl = $"https://portal.azure.com/#panelId/resource/search?q={Uri.EscapeDataString(db.name)}";
        var dbNode = parent.AddNode(
            $"{dbIcon}[white]SQL Database:[/] [link={portalUrl}][bold {NeonCyan}]{Markup.Escape(db.name)}[/][/]");

        dbNode.AddNode($"[{LightGrey}]Server:[/]   [{LightGrey}]{Markup.Escape(db.serverName)}[/]");

        var skuUrl = "https://azure.microsoft.com/en-us/pricing/details/azure-sql-database/single/";
        dbNode.AddNode($"[{LightGrey}]SKU:[/]      [link={skuUrl}][{NeonCyan}]{Markup.Escape(db.sqlDatabaseSku.sku)}[/][/] [{DarkGrey}]({Markup.Escape(db.sqlDatabaseSku.tier)})[/]");
        dbNode.AddNode(
            $"[{LightGrey}]Price:[/]    [{NeonCyan}]{FormatCurrencyPlain(db.sqlDatabaseSku.price)}[/] [{DarkGrey}]/{Markup.Escape(db.sqlDatabaseSku.priceUnit)}[/] " +
            $"[{DarkGrey}]([/][{CyberOrange}]{FormatCurrencyPlain(db.aggregateSqlDatabaseCosts.PerMonth)}[/][{DarkGrey}]/mo)[/]");
    }

    private static void AddSqlManagedInstanceNode(TreeNode parent, SqlManagedInstance mi)
    {
        var miIcon = SupportsUnicode ? "🗄️ " : "";

        var portalUrl = $"https://portal.azure.com/#panelId/resource/search?q={Uri.EscapeDataString(mi.name)}";
        var miNode = parent.AddNode(
            $"{miIcon}[white]SQL Managed Instance:[/] [link={portalUrl}][bold {NeonCyan}]{Markup.Escape(mi.name)}[/][/]");

        var skuUrl = "https://azure.microsoft.com/en-us/pricing/details/azure-sql-managed-instance/single/";
        miNode.AddNode($"[{LightGrey}]SKU:[/]          [link={skuUrl}][{NeonCyan}]{Markup.Escape(mi.sqlManagedInstanceSku.sku)}[/][/] [{DarkGrey}]({Markup.Escape(mi.sqlManagedInstanceSku.tier)})[/]");
        miNode.AddNode($"[{LightGrey}]vCores:[/]       [{CyberOrange}]{mi.sqlManagedInstanceSku.vCores}[/]");
        miNode.AddNode($"[{LightGrey}]Storage:[/]      [{LightGrey}]{mi.storageSizeInGB} GB[/]");
        miNode.AddNode($"[{LightGrey}]License:[/]      [{LightGrey}]{Markup.Escape(mi.licenseType)}[/]");
        miNode.AddNode(
            $"[{LightGrey}]Total Cost:[/]  [{CyberOrange}]{FormatCurrencyPlain(mi.aggregateSqlManagedInstanceCosts.PerMonth)}[/][{DarkGrey}]/mo[/]");
    }

    // ── Unsupported Resources ──────────────────────────────────────────────────

    private static void RenderUnsupportedResources(List<UnsupportedResource> resources)
    {
        var warningIcon = SupportsUnicode ? "⚠" : "[!]";
        AnsiConsole.MarkupLine(
            $"  [{DarkGrey}]{warningIcon}  {resources.Count} resource(s) were found but are not yet supported for cost estimation:[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.MinimalHeavyHead)
            .BorderColor(Color.Grey30);

        table.AddColumn(new TableColumn($"[{LightGrey}]Resource Name[/]"));
        table.AddColumn(new TableColumn($"[{LightGrey}]Resource Type[/]"));

        foreach (var resource in resources)
        {
            table.AddRow(
                $"[{NeonCyan}]{Markup.Escape(resource.resourceName)}[/]",
                $"[{DarkGrey}]{Markup.Escape(resource.resourceType)}[/]"
            );
        }

        AnsiConsole.Write(table);
    }

    // ── Budget Guard ───────────────────────────────────────────────────────────

    private static void RenderBudgetGuard(decimal monthlyCost, decimal? budget = null)
    {
        var threshold = budget ?? DefaultBudgetLimit;
        var pct = Math.Min((double)(monthlyCost / threshold * 100.0m), 100.0);

        var budgetLabel = budget.HasValue
            ? $"{FormatCurrencyPlain(threshold)} (--budget) monthly budget ({pct:N0}%)"
            : $"{FormatCurrencyPlain(threshold)} default monthly budget ({pct:N0}%)";

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
            $"[{color}]{FormatCurrencyPlain(monthlyCost)}[/] [{LightGrey}]/[/] [{LightGrey}]{budgetLabel}[/]");
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
        table.AddRow(new Markup($"[{NeonCyan}]  --budget[/] [dim]<usd>[/]"),               new Markup($"[{LightGrey}]Maximum monthly cost in USD. Sets [/][white]BUDGET_EXCEEDED[/][{LightGrey}] in GitHub Actions when exceeded.[/]"));
        table.AddRow(new Markup($"[{NeonCyan}]  --out-markdown[/] [dim]<path>[/]"),         new Markup($"[{LightGrey}]Write a Markdown summary report to[/] [white]<path>[/] [dim](e.g. cost-report.md)[/]"));
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
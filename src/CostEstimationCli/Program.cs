using CostEstimationCli.Configuration;
using CostEstimationCli.Repositories;
using CostEstimationCli.Services;
using CostEstimationCli.Services.Providers;
using CostEstimationCli.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CostEstimationCli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var cliArgs = ParseArguments(args);

            var configuration = BuildConfiguration();

            var pulumiSettings = configuration.GetSection(PulumiSettings.SectionName);
            var isDemoMode = string.Equals(pulumiSettings["DemoMode"], "true", StringComparison.OrdinalIgnoreCase);

            var services = new ServiceCollection();
            ConfigureServices(services, configuration, isDemoMode);
            var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // ── Feature 4: Load in-repository config (.saasfactory-cost.json) ──
            var configLoader = serviceProvider.GetRequiredService<LocalProjectConfigLoader>();
            var localConfig  = await configLoader.LoadAsync(cliArgs.WorkingDirectory);

            // Resolve effective budget: CLI flag > local config > null
            var effectiveBudget = cliArgs.Budget
                ?? localConfig?.Budget?.MonthlyLimit;

            // Collect ignore lists from local config
            IReadOnlyList<string>? ignoreTypes = localConfig?.Ignore?.ResourceTypes?.Count > 0
                ? localConfig.Ignore.ResourceTypes
                : null;
            IReadOnlyList<string>? ignoreNames = localConfig?.Ignore?.ResourceNames?.Count > 0
                ? localConfig.Ignore.ResourceNames
                : null;

            // Detect or select provider (before showing UI, so we can pass it to RenderHeader)
            var detectionService = serviceProvider.GetRequiredService<ProviderDetectionService>();
            IInfrastructureProvider? provider;

            if (cliArgs.Provider != null)
            {
                provider = detectionService.GetProviderByName(cliArgs.Provider);
                if (provider == null)
                {
                    ConsoleRenderer.RenderError(
                        "Unknown Provider",
                        $"'{cliArgs.Provider}' is not a recognised provider.",
                        "Available providers:  pulumi  bicep  cdk");
                    return 1;
                }
            }
            else if (isDemoMode)
            {
                provider = detectionService.GetProviderByName("Pulumi")!;
            }
            else
            {
                provider = await detectionService.DetectProviderAsync();
                if (provider == null)
                {
                    ConsoleRenderer.RenderError(
                        "No Provider Detected",
                        "Could not detect an IaC provider in the current directory.",
                        "Supported:  Pulumi (Pulumi.yaml)  ·  Azure Bicep (*.bicep)  ·  AWS CDK (cdk.out/ or cdk.json)\n" +
                        "Use --provider <name> to specify manually.");
                    return 1;
                }
            }

            // Render the premium header + environment card
            ConsoleRenderer.RenderHeader(configuration, provider);

            // ── Feature 3: Wire up DeltaCalculationService ─────────────────────
            var apiRepo       = serviceProvider.GetRequiredService<IApiRepository>();
            var costEstLogger = serviceProvider.GetRequiredService<ILogger<CostEstimationService>>();
            var deltaLogger   = serviceProvider.GetRequiredService<ILogger<DeltaCalculationService>>();
            var deltaService  = new DeltaCalculationService(apiRepo, deltaLogger);
            var costEstimationService = new CostEstimationService(
                provider, apiRepo, costEstLogger, deltaService);

            // Run estimation behind a premium live spinner
            var costEstimate = await ConsoleRenderer.ExecuteWithStatusAsync(
                $"Running cost estimation with {provider.DisplayName}…",
                async updateStatus =>
                {
                    if (isDemoMode && provider.Name == "Pulumi")
                        updateStatus("Loading sample data from demo files…");
                    else
                        updateStatus($"Extracting {provider.Name} resources…");

                    await Task.Delay(300);

                    updateStatus("Sending payload to Cloudcostify API…");
                    var result = await costEstimationService.EstimateCostAsync(
                        ignoreTypes, ignoreNames);

                    updateStatus("Receiving cost estimate…");
                    await Task.Delay(150);

                    return result;
                });

            ConsoleRenderer.RenderResults(costEstimate, effectiveBudget);

            // ── Feature 1: GitHub Actions modern file-based outputs ────────────
            var isGitHubActions = string.Equals(
                Environment.GetEnvironmentVariable(EnvironmentVariables.GITHUB_ACTIONS),
                "true", StringComparison.OrdinalIgnoreCase);

            var githubEnvFile    = Environment.GetEnvironmentVariable(EnvironmentVariables.GITHUB_ENV);
            var githubOutputFile = Environment.GetEnvironmentVariable(EnvironmentVariables.GITHUB_OUTPUT);

            if (!string.IsNullOrEmpty(githubEnvFile) || !string.IsNullOrEmpty(githubOutputFile))
            {
                // budget_exceeded is true only when a limit is configured AND the estimate exceeds it;
                // when no budget is set the output is always "false" so downstream steps can branch safely.
                var budgetExceeded    = effectiveBudget.HasValue &&
                                        costEstimate.aggregateCosts.PerMonth > effectiveBudget.Value;
                var budgetExceededStr = budgetExceeded ? "true" : "false";

                if (!string.IsNullOrEmpty(githubEnvFile))
                    await File.AppendAllTextAsync(githubEnvFile, $"BUDGET_EXCEEDED={budgetExceededStr}\n");

                if (!string.IsNullOrEmpty(githubOutputFile))
                    await File.AppendAllTextAsync(githubOutputFile, $"budget_exceeded={budgetExceededStr}\n");

                logger.LogInformation(
                    "GitHub Actions outputs written: budget_exceeded={Exceeded} " +
                    "(${Actual:N2}/mo vs ${Limit} limit).",
                    budgetExceededStr,
                    costEstimate.aggregateCosts.PerMonth,
                    effectiveBudget.HasValue ? $"{effectiveBudget.Value:N2}" : "none");
            }

            // ── Feature 2: Markdown report ────────────────────────────────────
            var markdownPath = cliArgs.MarkdownOutputPath;

            // Auto-generate a report when running inside a GitHub Actions PR context
            // and no explicit path was provided — write to a default location.
            if (string.IsNullOrEmpty(markdownPath) && isGitHubActions)
            {
                var eventName = Environment.GetEnvironmentVariable(EnvironmentVariables.GITHUB_EVENT_NAME);
                if (string.Equals(eventName, "pull_request", StringComparison.OrdinalIgnoreCase))
                    markdownPath = "cloudcostify-report.md";
            }

            if (!string.IsNullOrEmpty(markdownPath))
            {
                var markdownGenerator = new MarkdownReportGenerator();
                var markdownContent   = markdownGenerator.Generate(costEstimate, effectiveBudget);

                await File.WriteAllTextAsync(markdownPath, markdownContent);
                logger.LogInformation("Markdown report written to {Path}", markdownPath);
            }

            logger.LogInformation("Cost estimation completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleRenderer.RenderError("Unexpected Error", ex.Message, ex.ToString());
            return 1;
        }
    }


    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

        // Always add environment variables; they override JSON settings in both
        // CI/CD (TF_BUILD, GITHUB_ACTIONS) and local development.
        builder.AddEnvironmentVariables();

        var config = builder.Build();

        // Map well-known environment variables to typed configuration paths
        var inMemorySettings = new Dictionary<string, string?>();

        var baseUrl = Environment.GetEnvironmentVariable(EnvironmentVariables.CLOUDCOSTIFY_BASE_URL);
        if (!string.IsNullOrEmpty(baseUrl))
            inMemorySettings[$"{CostEstimationSettings.SectionName}:BaseUrl"] = baseUrl;

        var apiKey = Environment.GetEnvironmentVariable(EnvironmentVariables.CLOUDCOSTIFY_API_KEY);
        if (!string.IsNullOrEmpty(apiKey))
            inMemorySettings[$"{CostEstimationSettings.SectionName}:ApiKey"] = apiKey;

        var stackName = Environment.GetEnvironmentVariable(EnvironmentVariables.CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME);
        if (!string.IsNullOrEmpty(stackName))
            inMemorySettings[$"{PulumiSettings.SectionName}:StackName"] = stackName;

        var projectPath = Environment.GetEnvironmentVariable(EnvironmentVariables.CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH);
        if (!string.IsNullOrEmpty(projectPath))
            inMemorySettings[$"{PulumiSettings.SectionName}:ProjectDirectoryPath"] = projectPath;

        var projectName = Environment.GetEnvironmentVariable(EnvironmentVariables.PULUMI_PROJECT_NAME);
        if (!string.IsNullOrEmpty(projectName))
            inMemorySettings[$"{PulumiSettings.SectionName}:ProjectName"] = projectName;

        if (inMemorySettings.Count > 0)
        {
            builder.AddInMemoryCollection(inMemorySettings);
            config = builder.Build();
        }

        return config;
    }

    private static (string? Provider, string? WorkingDirectory, decimal? Budget, string? MarkdownOutputPath)
        ParseArguments(string[] args)
    {
        string? provider         = null;
        string? workingDir       = null;
        decimal? budget          = null;
        string? markdownOutPath  = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--provider" && i + 1 < args.Length)
            {
                provider = args[i + 1];
                i++;
            }
            else if (args[i] == "--directory" && i + 1 < args.Length)
            {
                workingDir = args[i + 1];
                i++;
            }
            else if (args[i] == "--budget" && i + 1 < args.Length)
            {
                if (decimal.TryParse(args[i + 1], System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    budget = parsed;
                i++;
            }
            else if (args[i] == "--out-markdown" && i + 1 < args.Length)
            {
                markdownOutPath = args[i + 1];
                i++;
            }
            else if (args[i] == "--help" || args[i] == "-h")
            {
                ConsoleRenderer.RenderHelp();
                Environment.Exit(0);
            }
        }

        return (provider, workingDir, budget, markdownOutPath);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration, bool isDemoMode)
    {
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        services.Configure<CostEstimationSettings>(configuration.GetSection(CostEstimationSettings.SectionName));
        services.Configure<PulumiSettings>(configuration.GetSection(PulumiSettings.SectionName));

        services.AddTransient<PulumiProvider>();
        services.AddTransient<BicepProvider>();
        services.AddTransient<CdkProvider>();

        services.AddTransient<IEnumerable<IInfrastructureProvider>>(sp => new IInfrastructureProvider[]
        {
            sp.GetRequiredService<PulumiProvider>(),
            sp.GetRequiredService<BicepProvider>(),
            sp.GetRequiredService<CdkProvider>()
        });

        services.AddTransient<ProviderDetectionService>();
        services.AddTransient<LocalProjectConfigLoader>();

        // Demo mode only controls the data source (file vs. real Pulumi stack).
        // The real API is always called — DemoApiRepository is never used.
        services.AddHttpClient<IApiRepository, ApiRepository>();
    }
}

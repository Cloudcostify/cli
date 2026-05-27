<<<<<<< HEAD
# Cloudcostify CLI
=======
# Cost estimation CLI
>>>>>>> e65b9b03dbb4f16b9c411d7c21f5e53591a15edb

[![Build and Test](https://github.com/cloudcostify/cli/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/cloudcostify/cli/actions/workflows/build-and-test.yml)
[![License](https://img.shields.io/badge/license-Custom-blue.svg)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)


<img width="696" height="709" alt="image" src="https://github.com/user-attachments/assets/7277a15e-df0a-4a95-add8-b706e7710476" />


> Pulumi cost estimation CLI for consistent cost estimation in your CI/CD pipeline

This tool ensures consistent cloud infrastructure cost estimation in your release pipeline running Pulumi infrastructure code before deploying resources. Get cost estimates for your infrastructure changes before they hit production.

## Features

- 🚀 **CI/CD Integration**: Works seamlessly in Azure DevOps, GitHub Actions, and other CI/CD platforms
- ☁️ **Multi-Cloud Support**: Azure and AWS resources (with more providers coming)
- 📊 **Detailed Cost Breakdown**: Get costs per hour, day, week, month, quarter, and year
- 🎨 **Rich Console Output**: Beautiful terminal output with tables and formatting
- 🔧 **Flexible Configuration**: Configure via appsettings.json or environment variables
- 🧪 **Well-Tested**: Comprehensive unit test coverage with TUnit
- 🏗️ **Modern Architecture**: Built with dependency injection and clean architecture principles

## Supported Resources

### Azure
- AKS Managed Clusters
- Virtual Machine SKUs
- Node Pools

### AWS
- Limited support (expanding)

More resources and providers coming soon!

## Installation

### Prerequisites

- .NET 10.0 SDK or later
- Pulumi CLI installed (not required for demo mode)
- Valid Cloudcostify API key (sign up at [cloudcostify.app](https://cloudcostify.app/))

### Demo Mode (Quick Start)

Want to try it without setting up Pulumi? Enable demo mode:

```bash
git clone https://github.com/cloudcostify/cli.git
cd cli
dotnet run --project src/CostEstimationCli/CostEstimationCli.csproj
```

Demo mode is pre-configured in `appsettings.Development.json`. See [DEMO_MODE.md](DEMO_MODE.md) for details.

### From Source

```bash
git clone https://github.com/cloudcostify/cli.git
cd cli
dotnet build
```

### Run Locally

```bash
dotnet run --project src/CostEstimationCli/CostEstimationCli.csproj
```

## Configuration

### Option 1: appsettings.json

Create or modify `appsettings.json`:

```json
{
  "CostEstimation": {
    "BaseUrl": "https://beta-api.cloudcostify.app/api/costestimate",
    "ApiKey": "your-api-key-here",
    "Authentication": {
      "Enabled": false
    }
  },
  "Pulumi": {
    "ProjectDirectoryPath": "./your-pulumi-project",
    "StackName": "dev",
    "ProjectName": "YourProject",
    "DemoMode": false
  }
}
```

### Option 2: Environment Variables

Set the following environment variables:

| Variable | Description | Required |
|----------|-------------|----------|
| `CLOUDCOSTIFY_BASE_URL` | API endpoint URL | Yes |
| `CLOUDCOSTIFY_API_KEY` | Your API subscription key | Yes |
| `CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME` | Pulumi stack name (e.g., "dev") | Yes |
| `CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH` | Path to Pulumi project | Yes |
| `PulumiProjectName` | Pulumi project name | Yes |
| `PULUMI_ACCESS_TOKEN` | Pulumi access token | Yes |

**Example (PowerShell):**

```powershell
$env:CLOUDCOSTIFY_BASE_URL = "https://beta-api.cloudcostify.app/api/costestimate"
$env:CLOUDCOSTIFY_API_KEY = "your-api-key"
$env:CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME = "dev"
$env:CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH = "./infrastructure"
$env:PulumiProjectName = "MyProject"
$env:PULUMI_ACCESS_TOKEN = "your-pulumi-token"
```

**Example (Bash):**

```bash
export CLOUDCOSTIFY_BASE_URL="https://beta-api.cloudcostify.app/api/costestimate"
export CLOUDCOSTIFY_API_KEY="your-api-key"
export CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME="dev"
export CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH="./infrastructure"
export PulumiProjectName="MyProject"
export PULUMI_ACCESS_TOKEN="your-pulumi-token"
```

## Usage

### Basic Usage

```bash
dotnet run --project src/CostEstimationCli/CostEstimationCli.csproj
```

### Azure DevOps Pipeline

```yaml
steps:
  - task: UseDotNet@2
    displayName: 'Use .NET 10'
    inputs:
      version: '10.0.x'

  - task: DotNetCoreCLI@2
    displayName: 'Run Cost Estimation'
    inputs:
      command: 'run'
      projects: 'path/to/CostEstimationCli.csproj'
    env:
      CLOUDCOSTIFY_BASE_URL: $(CLOUDCOSTIFY_BASE_URL)
      CLOUDCOSTIFY_API_KEY: $(CLOUDCOSTIFY_API_KEY)
      CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME: $(STACK_NAME)
      CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH: $(Build.SourcesDirectory)/infrastructure
      PulumiProjectName: $(PROJECT_NAME)
      PULUMI_ACCESS_TOKEN: $(PULUMI_ACCESS_TOKEN)
```

### GitHub Actions

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'

- name: Run Cost Estimation
  run: dotnet run --project src/CostEstimationCli/CostEstimationCli.csproj
  env:
    CLOUDCOSTIFY_BASE_URL: ${{ secrets.CLOUDCOSTIFY_BASE_URL }}
    CLOUDCOSTIFY_API_KEY: ${{ secrets.CLOUDCOSTIFY_API_KEY }}
    CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME: ${{ secrets.STACK_NAME }}
    CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH: ./infrastructure
    PulumiProjectName: ${{ secrets.PROJECT_NAME }}
    PULUMI_ACCESS_TOKEN: ${{ secrets.PULUMI_ACCESS_TOKEN }}
```

## Example Output

```
╭─────────────────────────────────────╮
│   Cost Estimation Results           │
╰─────────────────────────────────────╯

╭──────────────┬──────────────────╮
│ Time Period  │      Cost        │
├──────────────┼──────────────────┤
│ Per Hour     │ $2.50 USD        │
│ Per Day      │ $60.00 USD       │
│ Per Week     │ $420.00 USD      │
│ Per Month    │ $1,825.00 USD    │
│ Per Year     │ $21,900.00 USD   │
╰──────────────┴──────────────────╯

Cloud Provider: Azure
Currency: USD
```

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture

The CLI is built using modern .NET practices:

- **Dependency Injection**: Full DI container with service registration
- **Configuration Management**: Flexible configuration via JSON and environment variables
- **Repository Pattern**: Clean separation of data access
- **Service Layer**: Business logic encapsulation
- **Comprehensive Testing**: Unit tests with TUnit, NSubstitute, and FluentAssertions

### Project Structure

```
src/
├── CostEstimationCli/
│   ├── Configuration/      # Configuration classes
│   ├── Models/             # Data models
│   ├── Repositories/       # API communication
│   ├── Services/           # Business logic
│   └── Program.cs          # Entry point
└── CostEstimationCli.Tests/
    ├── Repositories/       # Repository tests
    └── Services/           # Service tests
```

## Contributing

We ❤️ contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on:

- Setting up your development environment
- Code style guidelines
- Submitting pull requests
- Reporting bugs
- Requesting features

## Community

- **Website**: [cloudcostify.app](https://cloudcostify.app/)
- **Slack**: [cloudcostify.slack.com](https://cloudcostify.slack.com)
- **Issues**: [GitHub Issues](https://github.com/cloudcostify/cli/issues)

## Roadmap

- [ ] Expanded AWS resource support
- [ ] Google Cloud Platform (GCP) support
- [ ] Cost optimization recommendations
- [ ] Historical cost tracking
- [ ] Budget alerts and notifications
- [ ] Docker image distribution
- [ ] Cross-platform binaries (Windows, Linux, macOS)
- [ ] Interactive mode
- [ ] Cost comparison between stacks

## License

This project is licensed under a custom license - see the [LICENSE](LICENSE) file for details.

**TL;DR**: Free to use, fork, and modify for personal and commercial projects. Cannot be sold as a standalone product.

## Acknowledgments

- Built with [Pulumi Automation API](https://www.pulumi.com/docs/using-pulumi/automation-api/)
- Powered by [Spectre.Console](https://spectreconsole.net/) for beautiful terminal output
- Tested with [TUnit](https://github.com/thomhurst/TUnit)

## Support

If you encounter issues or have questions:

1. Check the [documentation](https://cloudcostify.app/)
2. Search [existing issues](https://github.com/cloudcostify/cli/issues)
3. Join our [Slack community](https://cloudcostify.slack.com)
4. Create a [new issue](https://github.com/cloudcostify/cli/issues/new/choose)

---

Made with ❤️ by the Cloudcostify team

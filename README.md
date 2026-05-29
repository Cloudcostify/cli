<img width="699" height="1185" alt="image" src="https://github.com/user-attachments/assets/d58481f7-5a9a-4e61-a02c-1a9a8cf08da9" />

[![Build and Test](https://github.com/cloudcostify/cli/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/cloudcostify/cli/actions/workflows/build-and-test.yml)
[![License](https://img.shields.io/badge/license-Custom-blue.svg)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)


> Pulumi cost estimation CLI for consistent cost estimation in your CI/CD pipeline

This tool ensures consistent cloud infrastructure cost estimation in your release pipeline running Pulumi infrastructure code before deploying resources. Get cost estimates for your infrastructure changes before they hit production.

## Features

- 🚀 **CI/CD Integration**: Works seamlessly in GitHub Actions
- ☁️ **Multi-Cloud Support**: Azure resources (with more providers coming)
- 📊 **Detailed Cost Breakdown**: Get costs per hour, day, week, month, quarter, and year
- 🎨 **Rich Console Output**: Beautiful terminal output with tables and formatting
- 🔧 **Flexible Configuration**: Configure via appsettings.json or environment variables

## Supported Infrastructure as Code frameworks

| Framework | Supported |
|---|---|
| Pulumi  | ✅ - All Languages |
| AWS CDK | Coming soon |
| Bicep | Coming soon |

More infrastructure as code frameworks coming soon — let us know which you need

## Supported Resources

| Resource | Provider | Supported |
|---|---|---|
| AKS Managed Cluster | Azure | ✅ |
| AKS Node Pool | Azure | ✅ |
| Virtual Machine | Azure | ✅ |
| Virtual Machine Scale Set | Azure | ✅ |
| SQL Database | Azure | ✅ |
| Managed SQL Database | Azure | ✅ |
| EC2 Instance | AWS | Coming soon |
| RDS | AWS | Coming soon |

More resources and providers coming soon — let us know which you need

## Installation

### Prerequisites

- .NET 10.0 SDK or later
- Pulumi CLI installed (not required for demo mode)
- Valid Cloudcostify API key (sign up at [cloudcostify.io](https://cloudcostify.io/))

### NuGet Global Tool

Install the published prerelease globally:

```bash
dotnet tool install --global Cloudcostify.Cli --version 1.0.0-beta1 --prerelease
```

Update an existing installation:

```bash
dotnet tool update --global Cloudcostify.Cli --version 1.0.0-beta1 --prerelease
```

Run the tool:

```bash
cloudcostify
```

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

### Build a Local Tool Package

```bash
dotnet pack src/CostEstimationCli/CostEstimationCli.csproj --configuration Release --output ./artifacts/nupkg
```

Install the local package without publishing to NuGet:

```bash
dotnet tool install --tool-path ./.tools/cloudcostify --add-source ./artifacts/nupkg Cloudcostify.Cli --version 1.0.0-beta1
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

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Packing the Global Tool

```bash
dotnet pack src/CostEstimationCli/CostEstimationCli.csproj --configuration Release
```

### Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Contributing

We love contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on:

- Setting up your development environment
- Code style guidelines
- Submitting pull requests
- Reporting bugs
- Requesting features

## Community

- **Website**: [cloudcostify.io](https://cloudcostify.io/)
- **Slack**: [cloudcostify.slack.com](https://cloudcostify.slack.com)
- **Issues**: [GitHub Issues](https://github.com/cloudcostify/cli/issues)

## Roadmap

- [ ] AWS resource support
- [ ] Google Cloud Platform (GCP) support
- [ ] Budget alerts and notifications
- [ ] Docker image distribution
- [ ] Cross-platform binaries (Windows, Linux, macOS)
- [ ] Interactive mode
- [ ] Cost comparison between stacks

## License

This project is licensed under a custom license - see the [LICENSE](LICENSE) file for details.

**TL;DR**: Free to use, fork, and modify for personal and commercial projects. Cannot be sold as a standalone product.

## Support

If you encounter issues or have questions:

1. Check the [documentation](https://cloudcostify.io/)
2. Search [existing issues](https://github.com/cloudcostify/cli/issues)
3. Create a [new issue](https://github.com/cloudcostify/cli/issues/new/choose)
4. Reach out on email to hello@cloudcostify.io

---

Engineered with purpose by the Cloudcostify team

<img width="699" height="1185" alt="image" src="https://github.com/user-attachments/assets/d58481f7-5a9a-4e61-a02c-1a9a8cf08da9" />

[![Build and Test](https://github.com/cloudcostify/cli/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/cloudcostify/cli/actions/workflows/build-and-test.yml)
[![License](https://img.shields.io/badge/license-Custom-blue.svg)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)


> Pulumi cost estimation CLI for consistent cost estimation in your CI/CD pipeline

This tool ensures consistent cloud infrastructure cost estimation in your release pipeline running Pulumi infrastructure code before deploying resources. Get cost estimates for your infrastructure changes before they hit production.

---

## 📖 Documentation

- 🚀 [Installation & Quick Start](#installation)
- 🏗️ [Supported IaC Frameworks](docs/supported-iac-frameworks.md)
- ☁️ [Supported Cloud Resources](docs/supported-cloud-resources.md)

## Installation

### Prerequisites

- [.NET 10 or later](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) - (Required for local CLI execution only)
- Valid [**Cloudcostify API key**](https://cloudcostify.io)

## Run the CLI locally

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

## Run the GitHub Action

```yaml

- name: Cloudcostify Budget Guard
  uses: Cloudcostify/github-action@v1.0.0-beta
  env:
    CLOUDCOSTIFY_API_KEY: ${{ secrets.CLOUDCOSTIFY_API_KEY }}
    CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME: ${{ secrets.STACK_NAME }}
    CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH: ./infrastructure
    PulumiProjectName: ${{ secrets.PROJECT_NAME }}
```

## Configuration

Set the following environment variables:

| Variable | Description | Required |
|----------|-------------|----------|
| `CLOUDCOSTIFY_API_KEY` | Your API subscription key | Yes |
| `CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME` | Pulumi stack name (e.g., "dev") | Yes |
| `CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH` | Path to Pulumi project | Yes |
| `PulumiProjectName` | Pulumi project name | Yes |

**Example (PowerShell):**

```powershell
$env:CLOUDCOSTIFY_API_KEY = "your-api-key" 
$env:CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME = "dev" 
$env:CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH = "./infrastructure" 
$env:PulumiProjectName = "MyProject"
```

**Example (Bash):**

```bash
export CLOUDCOSTIFY_API_KEY="your-api-key" 
export CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME="dev" 
export CLOUDCOSTIFY_PULUMI_PROJECT_DIRECTORY_PATH="./infrastructure" 
export PulumiProjectName="MyProject"
```

## Security & Privacy

We only transmit an anonymous list of resource types and quantities (e.g., '1x Standard_D2_v2 VM, 1x AKS Cluster') required to calculate the cost. Your source code, variables, and cloud credentials never leave your infrastructure. You can verify this by reviewing the [CLI](https://github.com/cloudcostify/cli) and [GitHub Action](https://github.com/cloudcostify/github-action) source code on GitHub.

We take security and privacy seriously. For any security concerns, please contact us at <mailto:hello@cloudcostify.io>.

## Contributing

We love contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on:

- Setting up your development environment
- Code style guidelines
- Submitting pull requests
- Reporting bugs
- Requesting features

## License

This project is licensed under a custom license - see the [LICENSE](LICENSE) file for details.

**TL;DR**: Free to use, fork, and modify for personal and commercial projects. Cannot be sold as a standalone product.

## Community & Support

If you encounter issues or have questions:

- **Issues**: [GitHub Issues](https://github.com/cloudcostify/cli/issues)
- **Discussions**: [GitHub Discussions](https://github.com/cloudcostify/cli/discussions)
- **Contact**: <mailto:hello@cloudcostify.io>

---

Engineered with purpose by the Cloudcostify team

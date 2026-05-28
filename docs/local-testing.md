# Local Testing Guide

End-to-end testing of the Cloudcostify CLI against the PulumiSandbox sample project without deploying anything to Azure.

---

## Prerequisites

| Tool | Install | Notes |
|---|---|---|
| .NET 10 SDK | `winget install Microsoft.DotNet.SDK.10` | Required to build and run the CLI |
| Pulumi CLI | `winget install pulumi` | Required to generate preview JSON |
| Azure CLI | `winget install Microsoft.AzureCLI` | Required for AzureNative resource planning |
| Pulumi account or local backend | `pulumi login --local` | Local backend avoids needing a Pulumi cloud account |

Verify tools are available:
```powershell
pulumi version
az version
dotnet --version
```

---

## Step 1 — Azure login

The Pulumi AzureNative provider needs Azure credentials to plan resource changes (even for a dry-run preview).

```powershell
az login
```

---

## Step 2 — Initialise the PulumiSandbox stack (first time only)

```powershell
cd samples/PulumiSandbox

# Use local file backend — no Pulumi cloud account required
pulumi login --local

# Set a passphrase for the local secrets backend and remember it
$env:PULUMI_CONFIG_PASSPHRASE = "cloudcostify"

# Create the dev stack
pulumi stack init dev
```

> **Note:** `PULUMI_CONFIG_PASSPHRASE` must be set to the same value every time you run Pulumi commands against this stack. The passphrase is used to derive an encryption key that is stored in `Pulumi.dev.yaml`. If you lose the passphrase, delete `encryptionsalt` from `Pulumi.dev.yaml` and re-run `pulumi stack init dev`.

---

## Step 3 — Generate the preview snapshot

```powershell
cd samples/PulumiSandbox

$env:PULUMI_CONFIG_PASSPHRASE = "cloudcostify"

pulumi preview --json 2>&1 | Tee-Object preview-output.json
if ($LASTEXITCODE -eq 0) { Copy-Item preview-output.json ../pulumi-preview.json }
```

`pulumi preview --json` is a **dry-run** — it calculates what would be created but makes no changes in Azure. The output is a JSON document with a `steps` array containing one entry per resource with an `op` field (`create`, `update`, `delete`, `same`).

Using `Tee-Object` instead of `>` prevents overwriting `pulumi-preview.json` with an empty file when the command fails.

---

## Step 4 — Run the CLI

```powershell
# Run from the repo root
$env:CLOUDCOSTIFY_BASE_URL = "https://your-api-url"
$env:CLOUDCOSTIFY_API_KEY  = "your-api-key"

dotnet run --project src/CostEstimationCli
```

Demo mode is enabled in `src/CostEstimationCli/appsettings.json` by default (`"DemoMode": true`). The CLI reads `samples/pulumi-preview.json` as the infrastructure input and calls the real Cloudcostify API.

### Optional flags

```powershell
# Write a Markdown cost report
dotnet run --project src/CostEstimationCli -- --out-markdown cost-report.md

# Override the budget threshold
dotnet run --project src/CostEstimationCli -- --budget 500
```

### Simulating GitHub Actions outputs locally

```powershell
$env:GITHUB_ACTIONS   = "true"
$env:GITHUB_ENV       = "$env:TEMP\gh_env.txt"
$env:GITHUB_OUTPUT    = "$env:TEMP\gh_output.txt"
$env:BUDGET           = "500"

dotnet run --project src/CostEstimationCli -- --budget 500

# Inspect written outputs
Get-Content $env:GITHUB_ENV
Get-Content $env:GITHUB_OUTPUT
```

---

## Troubleshooting

### `appsettings.json` not found when running from repo root
Caused by old versions of the CLI using `Directory.GetCurrentDirectory()` as the config base path. Fixed in current code (`AppContext.BaseDirectory`). Run `dotnet build` to pick up the fix.

### `pulumi-preview.json` is 0 bytes after running `pulumi preview`
The redirect operator `>` creates the file before the command runs. If Pulumi exits with an error, the file is empty. Always use `Tee-Object` with `$LASTEXITCODE` guard (Step 3 above).

The repo ships a reference snapshot at `samples/pulumi-preview.json` which is sufficient for demo mode and all CLI features. Only regenerate it when you want to reflect changes to `MyStack.cs`.

### `incorrect passphrase` on `pulumi stack init dev`
The `encryptionsalt` line in `Pulumi.dev.yaml` was written by a previous `stack init` with a different passphrase. Remove it and re-init:
```powershell
# Edit samples/PulumiSandbox/Pulumi.dev.yaml and delete the encryptionsalt line, leaving:
#   config:
#     azure-native:location: WestEurope

$env:PULUMI_CONFIG_PASSPHRASE = "cloudcostify"
pulumi stack init dev
```

### `stack 'organization/PulumiSandbox/dev' already exists`
Skip `pulumi stack init dev` — the stack was already created. If you need to reset it:
```powershell
Remove-Item "$env:USERPROFILE/.pulumi/stacks/organization/PulumiSandbox" -Recurse -Force -ErrorAction SilentlyContinue
# Then remove encryptionsalt from Pulumi.dev.yaml, and re-run stack init
```

### `az: executable file not found in %PATH%`
Azure CLI is not installed or the terminal was not restarted after installation.
```powershell
winget install Microsoft.AzureCLI
# Restart terminal, then:
az login
```
The preview will still partially succeed (the Pulumi stack root resource is created), but Azure-specific resources (`azure-native:*`, `azuread:*`) will not be planned. Use the restored sample file for full resource coverage in the meantime.

### `pulumi: no Pulumi.yaml project file found`
You ran `pulumi preview` from the wrong directory. Always run from `samples/PulumiSandbox/` where `Pulumi.yaml` lives, not from the repo root.

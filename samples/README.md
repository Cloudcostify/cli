# Sample Files

This directory contains sample files and the PulumiSandbox project used for testing and demonstration purposes.

## Files

### pulumi-preview.json
Captured output from `pulumi preview --json` against the PulumiSandbox stack (AKS cluster + ResourceGroup).
Used by the CLI's demo mode (`DemoMode: true`) and as input for the delta calculation service.

### cloudresources.json
Sample cloud resource configuration showing the structure expected by the cost estimation API.

### Pulumi.yaml
Minimal Pulumi project configuration reference.

---

## PulumiSandbox

A real Pulumi project (AKS cluster) used to generate test preview snapshots without deploying anything to Azure.

**Resources defined:**
- `azure-native:resources:ResourceGroup`
- `azure-native:containerservice:ManagedCluster` (Standard_B2s nodes, autoscale 1–3)
- `azuread:Application` + `ServicePrincipal`
- `tls:PrivateKey` (SSH key)
- `random:RandomPassword`

### Regenerating pulumi-preview.json

Run from the `PulumiSandbox/` directory. This performs a dry-run — nothing is deployed.

**Requirements:**
- Pulumi CLI installed (`winget install pulumi`)
- Azure CLI logged in (`az login`) — needed for Azure Native resource planning
- .NET 8 SDK

```powershell
cd samples/PulumiSandbox

# Use local backend so no Pulumi account is needed
pulumi login --local

# Initialise the stack (first time only)
pulumi stack init dev

# Run the preview and capture JSON output
pulumi preview --json > ../pulumi-preview.json
```

The resulting `../pulumi-preview.json` has a `steps` array with `op` values (`create`, `update`, `delete`, `same`)
which the CLI's delta calculation service uses to compute net cost changes.

### Running the CLI against this snapshot

```powershell
# From the repo root
$env:CLOUDCOSTIFY_BASE_URL = "https://your-api-url"
$env:CLOUDCOSTIFY_API_KEY  = "your-key"

# Point demo mode at the snapshot
dotnet run --project src/CostEstimationCli -- --provider pulumi
```

Or enable demo mode in `appsettings.json`:
```json
"Pulumi": {
  "DemoMode": true,
  "DemoDataPath": "samples/pulumi-preview.json"
}
```

# Demo Mode

Demo mode allows you to run the CLI without a real Pulumi project or API credentials. Perfect for testing, development, and demonstrations.

## Enabling Demo Mode

### Option 1: Using appsettings.Development.json (Nemmeste!)

The repo includes `src/CostEstimationCli/appsettings.Development.json` with demo mode pre-configured:

```json
{
  "Pulumi": {
    "DemoMode": true,
    "DemoDataPath": "samples/pulumi-preview.json"
  }
}
```

Just run (from root directory):
```bash
cd Public/cli
dotnet run --project src/CostEstimationCli/CostEstimationCli.csproj
```

Or from the project directory:
```bash
cd Public/cli/src/CostEstimationCli
dotnet run
```

### Option 2: Edit appsettings.json

In `src/CostEstimationCli/appsettings.json`, set:

```json
{
  "Pulumi": {
    "DemoMode": true,
    "DemoDataPath": "samples/pulumi-preview.json"
  }
}
```

### Option 3: Environment Variable

Set via environment variable (overrides appsettings):

**PowerShell:**
```powershell
$env:Pulumi__DemoMode = "true"
$env:Pulumi__DemoDataPath = "samples/pulumi-preview.json"
dotnet run --project src/CostEstimationCli/CostEstimationCli.csproj
```

**Bash:**
```bash
export Pulumi__DemoMode=true
export Pulumi__DemoDataPath=samples/pulumi-preview.json
dotnet run --project src/CostEstimationCli/CostEstimationCli.csproj
```

## What Demo Mode Does

When demo mode is enabled:

1. ✅ **Skips Pulumi validation** - No need for `ProjectName`, `StackName`, or `ProjectDirectoryPath`
2. ✅ **Loads sample data** - Reads from `samples/pulumi-preview.json` instead of calling Pulumi Automation API
3. ✅ **Sends to your API** - Still sends the sample data to your cost estimation API (if configured)
4. ✅ **Displays results** - Shows the cost estimation output with Spectre.Console

## Demo Mode Output

When running in demo mode, you'll see:

```
========================================
DEMO MODE ENABLED
Using sample data from: samples/pulumi-preview.json
========================================
```

## Use Cases

- **Testing**: Test CLI without real Pulumi projects
- **Development**: Develop the CLI without Pulumi setup
- **Demonstrations**: Show the CLI in action without credentials
- **CI/CD testing**: Test the pipeline without real infrastructure

## Disabling Demo Mode

Set `DemoMode: false` in your configuration or remove the environment variable:

```json
{
  "Pulumi": {
    "DemoMode": false
  }
}
```

Or:
```bash
unset Pulumi__DemoMode  # Bash
Remove-Item Env:Pulumi__DemoMode  # PowerShell
```

## Custom Demo Data

You can use your own sample data by changing `DemoDataPath`:

```json
{
  "Pulumi": {
    "DemoMode": true,
    "DemoDataPath": "path/to/your/custom-data.json"
  }
}
```

The file must be valid JSON that Pulumi would export.

## Notes

- Demo mode still requires API configuration if you want to send data to your API
- The sample data in `samples/pulumi-preview.json` is from the original project
- Demo mode bypasses all Pulumi-related validation and setup

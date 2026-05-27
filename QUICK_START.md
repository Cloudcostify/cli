# Quick Start - Demo Mode

Kør CLI'en i demo mode uden Pulumi setup:

## Step 1: Naviger til mappen
```bash
cd D:\Development\Business-Projects\Cloudcostify\Public\cli
```

## Step 2: Kør CLI'en
```bash
dotnet run --project src/CostEstimationCli/CostEstimationCli.csproj
```

Det er det! 🎉

## Hvad sker der?

1. ✅ CLI'en læser `src/CostEstimationCli/appsettings.Development.json`
2. ✅ Ser at `DemoMode: true`
3. ✅ Loader sample data fra `samples/pulumi-preview.json`
4. ✅ Sender til dit API på `BaseUrl` (hvis konfigureret)
5. ✅ Viser resultatet med Spectre.Console

## Ændre API URL

Rediger `src/CostEstimationCli/appsettings.Development.json`:

```json
{
  "CostEstimation": {
    "BaseUrl": "http://localhost:5178/api/cost-estimates",  // ← Din API
    "ApiKey": "demo-key"
  }
}
```

## Se Output

Du vil se:
```
========================================
DEMO MODE ENABLED
Using sample data from: samples/pulumi-preview.json
========================================
```

## Test uden API

Hvis dit API ikke kører, vil du få en HTTP fejl - det er forventet. CLI'en sender altid data til API'et når BaseUrl er sat.

For at teste uden API, kan du sætte BaseUrl til en værdi der ikke findes, og så vil du se HTTP fejlen i stedet.

## Slå Demo Mode Fra

Når du vil bruge rigtig Pulumi:

1. Rediger `src/CostEstimationCli/appsettings.json`:
```json
{
  "Pulumi": {
    "DemoMode": false,
    "ProjectName": "YourProject",
    "StackName": "dev",
    "ProjectDirectoryPath": "./your-pulumi-project"
  }
}
```

2. Eller brug environment variables (se DEMO_MODE.md)

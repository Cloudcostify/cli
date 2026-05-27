# Live Spinner Feature

CLI'en bruger nu Spectre.Console's live spinner til at give visuelt feedback mens der hentes cost estimates.

## Hvad du ser

### 1. ASCII Art Banner
```
 ██████╗██╗      ██████╗ ██╗   ██╗██████╗  ██████╗ ██████╗ ███████╗████████╗██╗███████╗██╗   ██╗
██╔════╝██║     ██╔═══██╗██║   ██║██╔══██╗██╔════╝██╔═══██╗██╔════╝╚══██╔══╝██║██╔════╝╚██╗ ██╔╝
██║     ██║     ██║   ██║██║   ██║██║  ██║██║     ██║   ██║███████╗   ██║   ██║█████╗   ╚████╔╝
██║     ██║     ██║   ██║██║   ██║██║  ██║██║     ██║   ██║╚════██║   ██║   ██║██╔══╝    ╚██╔╝
╚██████╗███████╗╚██████╔╝╚██████╔╝██████╔╝╚██████╗╚██████╔╝███████║   ██║   ██║██║        ██║
 ╚═════╝╚══════╝ ╚═════╝  ╚═════╝ ╚═════╝  ╚═════╝ ╚═════╝ ╚══════╝   ╚═╝   ╚═╝╚═╝        ╚═╝
```

### 2. Animeret Spinner
Mens CLI'en arbejder, ser du en animeret spinner med status beskeder:

**Demo Mode:**
- 📂 Loader demo data fra samples...
- 📡 Sender data til Cloudcostify API...
- ✓ Modtager prisestimat...

**Production Mode:**
- 🔄 Eksporterer Pulumi stack data...
- 📡 Sender data til Cloudcostify API...
- ✓ Modtager prisestimat...

### 3. Pænt Formateret Output
```
╭─────────────┬──────────────────╮
│ Time Period │       Cost       │
├─────────────┼──────────────────┤
│  Per Hour   │   0,14 kr. USD   │
│   Per Day   │   3,46 kr. USD   │
│  Per Week   │  24,19 kr. USD   │
│  Per Month  │  103,68 kr. USD  │
│  Per Year   │ 1.261,44 kr. USD │
╰─────────────┴──────────────────╯
```

## Teknisk Implementation

### Spinner Configuration
```csharp
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)          // Animated dots
    .SpinnerStyle(Style.Parse("green bold")) // Green bold style
    .StartAsync("[green]Message[/]", async ctx =>
    {
        // Work happens here
        ctx.Status("[cyan]New status...[/]"); // Update status
        return result;
    });
```

### Log Level
For at undgå at log beskeder forstyrrer spinner animation, er log level sat til `Warning`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "CostEstimationCli": "Warning"
    }
  }
}
```

## Features

- ✅ Animeret dots spinner (grøn, bold)
- ✅ Dynamiske status opdateringer
- ✅ Emoji ikoner for bedre visuel feedback
- ✅ ASCII art banner ved start
- ✅ Farvet output
- ✅ Clean console uden log spam

## Se det i aktion

```bash
cd Public/cli/src/CostEstimationCli
dotnet run
```

Du vil se:
1. Cloudcostify ASCII art logo
2. Konfigurations info
3. Animeret spinner med status opdateringer
4. Pænt formateret cost tabel
5. Resource detaljer

Spinneren animerer automatisk mens API kald er i gang! 🎨✨

# Developer Guide

How UK Postcode Distance Calculator works and how to build it. For using the app, see the [README](../README.md).

It's a .NET 10 Windows Forms app with no NuGet dependencies beyond the test project. Each class has XML doc comments describing its role.

## How it works

1. **Parse** (`Helpers/JourneyParser.cs`): the input is split into journeys on semicolons and `date:` lines, and into postcodes on commas and new lines. Postcodes are upper-cased with spaces removed.
2. **Look up** (`Services/RouteApi.cs`): every unique postcode in the batch goes to postcodes.io in one bulk request. Postcodes not found are retried as retired postcodes, which still have a last known location.
3. **Route**: each journey is one OSRM request through all its stops, which returns every leg's distance. Requests are kept at least a second apart (`Services/RequestThrottle.cs`).
4. **Show** (`Helpers/BreakdownFormatter.cs`): the legs become the plain-text breakdown and totals. Distances are stored in metres and only converted to miles or km for display.

History is saved to `history.json` next to the exe after every change (`Services/HistoryStore.cs`). An unreadable file is renamed to `history.corrupt-<timestamp>.json` rather than overwritten. Display settings go to `settings.json` (`Services/SettingsStore.cs`); a missing or bad file just gives the defaults.

## APIs

Both are free, need no key, and are called over HTTPS with a 15 second timeout.

**[postcodes.io](https://postcodes.io/)**
- `POST https://api.postcodes.io/postcodes` with `{"postcodes": [...]}`, up to 100 per request. Unknown postcodes come back with `"result": null` and are all reported together.
- `GET https://api.postcodes.io/terminated_postcodes/{postcode}` for retired postcodes.

**[Project OSRM](https://project-osrm.org/)**
- `GET https://router.project-osrm.org/route/v1/driving/{lon},{lat};{lon},{lat};...?overview=false`, one per journey. Leg distances are in `routes[0].legs[i].distance` (metres).
- This is OSRM's public demo server. Its [usage policy](https://github.com/Project-OSRM/osrm-backend/wiki/Demo-server) allows one request per second and it isn't meant for heavy use. Anything larger than personal mileage tracking should run its own OSRM server.

**Google Maps** isn't called by the app. Verifying opens a [Maps URL](https://developers.google.com/maps/documentation/urls/get-started) in the browser (`Helpers/GoogleMapsLink.cs`) and the user types in the distance.

## Project structure

```text
UI/                      Main form, its designer file, verify dialog, icon font, warning text box
Models/                  Leg, Journey, JourneyBatch, DistanceUnit, PostcodeLocation
Helpers/                 Input parsing, breakdown text, Google Maps links, typo warnings, distance input
Services/                API calls, request throttle, history and settings files
Extensions/              Unit conversion and formatting
Resources/               Material Symbols icon subset and app icon, embedded in the exe
PostcodeDistanceCalculator.Tests/   xUnit tests (no live API calls)
Documentation/           This guide and the changelog (README.md is at the root)
```

Icons come from a subset of [Material Symbols](https://fonts.google.com/icons) embedded in the exe so they work offline. See `UI/IconFont.cs` for how to add one.

## Building

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download) on Windows.

```powershell
dotnet build
dotnet run --project PostcodeDistanceCalculator.csproj
dotnet test
```

Or open `PostcodeDistanceCalculator.slnx` in Visual Studio and press F5.

## Releasing

1. Update `<Version>` in `PostcodeDistanceCalculator.csproj` (shown in the exe's properties and the Help dialog) and date the Unreleased section of [CHANGELOG.md](CHANGELOG.md).
2. Build the single-file exe, which includes .NET so users don't need it installed:
   ```powershell
   dotnet publish PostcodeDistanceCalculator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
   ```
   Output: `bin/Release/net10.0-windows/win-x64/publish/PostcodeDistanceCalculator.exe`
3. Tag the commit with the version and attach the exe to a GitHub release.

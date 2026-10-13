# UK Postcode Distance Calculator

**Version 2.0.0** · [MIT License](LICENSE)

A lightweight, modern Windows Forms desktop application built with **.NET 10** to calculate driving distances between UK postcodes. The tool supports multi-stop routes, batch processing of multiple independent journeys, live unit conversion (miles / kilometers), interactive route breakdowns, saved history grouped into dated batches, and one-click clipboard copying. It is a portable app: history is saved next to the executable.

<img width="944" height="603" alt="UkPostcodeDistanceCalculator" src="https://github.com/user-attachments/assets/af25fc1f-2530-455d-bef3-2aae538715de" />

---

## Table of Contents

- [Overview](#overview)
- [Example Use Case: Monthly Mileage](#example-use-case-monthly-mileage)
- [Key Features](#key-features)
- [How It Works](#how-it-works)
  - [Architecture Workflow](#architecture-workflow)
  - [Processing Pipeline](#processing-pipeline)
- [External API Integrations](#external-api-integrations)
  - [1. Geocoding via postcodes.io](#1-geocoding-via-postcodesio)
  - [2. Driving Route Calculation via Project OSRM](#2-driving-route-calculation-via-project-osrm)
  - [Distance Calibration Factor (+7%)](#distance-calibration-factor-7)
- [Application Usage Guide](#application-usage-guide)
  - [Input Format & Syntax](#input-format--syntax)
  - [Date Tagging](#date-tagging)
  - [History Management](#history-management)
  - [Clipboard & Quick Actions](#clipboard--quick-actions)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Build and Publish Instructions](#build-and-publish-instructions)
  - [Local Development & Running](#local-development--running)
  - [Single-File Self-Contained Executable](#single-file-self-contained-executable)
  - [Publish Parameter Breakdown](#publish-parameter-breakdown)
- [Dependencies & Attribution](#dependencies--attribution)
- [Disclaimer](#disclaimer)
- [License](#license)

---

## Overview

Calculating driving distances across sequences of UK postcodes is a common requirement for travel planning, mileage expenses, logistics, and delivery estimates. 

**UK Postcode Distance Calculator** provides a clean, single-window graphical interface that:
1. Translates arbitrary UK postcodes into precise geographical coordinates using the free [postcodes.io](https://postcodes.io/) REST API.
2. Calculates real driving routes between consecutive waypoints using the [Project OSRM](https://project-osrm.org/) routing engine.
3. Automatically adjusts raw routing distances with a calibrated multiplier to closely reflect real-world navigation systems (e.g. Google Maps).
4. Maintains a saved history sidebar allowing users to compare routes, track cumulative distances, and export totals to the clipboard.

---

## Example Use Case: Monthly Mileage

A typical use is adding up all the driving done in a month to claim mileage:

1. After a few days of driving, enter each trip as its own journey in one batch, separated by semicolons:
   ```text
   SW1A 1AA, M1 1AE, SW1A 1AA ; SW1A 1AA, B1 1AA ; B1 1AA, LS1 1UR, SW1A 1AA
   ```
   Optionally put a date in the **Date** box (e.g. `14/10`) to tag the journeys with the day they happened.
2. Click **Calculate Route**, check the breakdown, then click **+ Save to History**. The batch is saved with today's date and time, so even undated journeys can be traced back to when they were entered.
3. Repeat through the month. History is kept in `history.json` next to the app, so it survives closing and reopening.
4. At month end, click **Copy** next to **History Total** to copy the month's total mileage (just the number, e.g. `412.6`) and paste it into the expense form or spreadsheet.
5. Click **Clear** in the history panel to start the next month fresh.

---

## Key Features

- **Multi-Leg Route Calculation**: Enter 2 or more UK postcodes to compute cumulative driving distance leg-by-leg.
- **Batch Journey Processing**: Separate multiple independent journeys using semicolons (`;`) to calculate them all in a single operation.
- **Dynamic Unit Switching**: Instantly switch between **Miles** and **Kilometers (km)** with automatic, live conversion of current results and saved history without re-querying the APIs.
- **Saved History, Grouped by Batch**:
  - Each save becomes a batch, automatically stamped with the date and time it was calculated.
  - Batches show as shaded headers with their journey count and total; their journeys are listed underneath.
  - History is saved to `history.json` next to the executable (portable, no AppData), so it persists between sessions and moves with the app folder.
  - Delete a single journey or a whole batch with the inline delete buttons, or **Clear** everything (e.g. at month end).
  - Running grand total across all saved journeys.
  - Collapsible panel with toggle button to maximize screen space.
- **One-Click Clipboard Export**: Click either total label or its **Copy** button to copy just the number (e.g. `123.4`, no label or unit) for spreadsheets or expense forms. A popup confirms exactly what was copied.
- **Optional Date Tagging**: Optional free-text date field; its placeholder hint shows today's date (`dd/MM`).
- **Resilient Error Handling & Live Status**: Provides feedback for network timeouts, unrecognized postcodes (HTTP 404), and unroutable coordinates.

---

## How It Works

### Architecture Workflow

```text
+------------------------------------------------------+
|                     User Input                       |
|        (Postcodes + Optional Date + Unit)            |
+--------------------------+---------------------------+
                           |
                           v
+------------------------------------------------------+
|             Input Parser & Normalizer                |
|  (Split ';' journeys, split ',' postcodes, uppercase) |
+--------------------------+---------------------------+
                           |
                           v
+------------------------------------------------------+
|           Coordinate Resolver (postcodes.io)          |
|             GET /postcodes/{postcode}                |
+--------------------------+---------------------------+
                           |
                           v
+------------------------------------------------------+
|          Routing Engine (Project OSRM)               |
|      GET /route/v1/driving/{lon},{lat};...           |
+--------------------------+---------------------------+
                           |
                           v
+------------------------------------------------------+
|             Distance Calibration Factor              |
|             (Meters * 1.07 multiplier)               |
+--------------------------+---------------------------+
                           |
                           v
+------------------------------------------------------+
|                     UI Renderer                      |
|         (Formatted Breakdown Table & Totals)         |
+------------------------------------------------------+
```

### Processing Pipeline

1. **Input Normalization**:
   - The raw text in the input box is split by semicolons (`;`) into individual journeys.
   - Each journey is split by commas (`,`), stripped of internal whitespace, and capitalized (e.g., `sw1a 1aa` becomes `SW1A1AA`).
   - Journeys with fewer than 2 postcodes are skipped.

2. **Deduplicated Geocoding**:
   - All unique postcodes across the journey are identified.
   - Coordinates are looked up one postcode at a time (each request is awaited, so the window stays responsive) and cached for the whole batch, so a postcode that appears in several journeys is only fetched once.

3. **Sequential Leg Routing**:
   - The app loops through postcodes `0` to `N - 1`, pairing adjacent postcodes (`postcodes[i]` $\to$ `postcodes[i + 1]`).
   - Each leg is sent to OSRM to determine the driving distance in meters.

4. **Formatting & Display**:
   - Legs are formatted into a structured ASCII/Unicode breakdown table showing leg numbers, start/end postcodes, individual leg distances, and overall totals.
   - The UI updates the `Journey Total` label, activates the `+ Save to History` button, and logs status messages.

---

## External API Integrations

The application communicates with two public REST APIs using a shared `HttpClient` instance (15 second timeout). Requests are made one at a time and awaited, so the window stays responsive while they run.

### 1. Geocoding via postcodes.io

- **Endpoint**: `https://api.postcodes.io/postcodes/{postcode}`
- **Method**: `GET`
- **Purpose**: Retrieves WGS84 geographic coordinates (latitude and longitude) for UK postcodes.
- **Response Format**: JSON containing a `result` object with `latitude` and `longitude` numeric fields.
- **Error Handling**:
  - **404 Not Found**: Specifically caught and displayed to the user as `'{postcode}' is not a recognized UK postcode.`
  - **Network Issues & Timeouts**: `HttpRequestException` and `TaskCanceledException` are caught with informative messages.
  - **JSON Parsing**: Uses `System.Text.Json.JsonDocument` with validation on the `result` property.

```csharp
// Example request:
// GET https://api.postcodes.io/postcodes/SW1A1AA
{
  "status": 200,
  "result": {
    "postcode": "SW1A 1AA",
    "latitude": 51.501009,
    "longitude": -0.141588
  }
}
```

### 2. Driving Route Calculation via Project OSRM

- **Endpoint**: `http://router.project-osrm.org/route/v1/driving/{startLon},{startLat};{endLon},{endLat}?overview=false`
- **Method**: `GET`
- **Purpose**: Computes the optimal driving distance between two coordinates based on OpenStreetMap road network data.
- **Request Headers**: A custom `User-Agent: WinFormsPostcodeDistanceTool` header identifies the app, as the OSRM usage policy asks.
- **Usage limits**: `router.project-osrm.org` is OSRM's public demo server. Its [usage policy](https://github.com/Project-OSRM/osrm-backend/wiki/Demo-server) allows at most one request per second and it isn't meant for production use, so heavy use may be rate-limited or blocked. That's fine for personal mileage tracking; anything larger should run its own OSRM server.
- **Formatting**: Coordinates are explicitly formatted using `CultureInfo.InvariantCulture` (`startLon.ToString(CultureInfo.InvariantCulture)`) to ensure periods (`.`) are used as decimal points regardless of the user's Windows locale.
- **Response Format**: JSON containing `routes[0].distance` in meters and a `code` string (`"Ok"`, `"NoRoute"`, etc.).
- **Error Handling**:
  - Checks if the response code is `"NoRoute"` and alerts the user that no drivable route exists between those points.
  - Verifies presence and valid array structure of `routes`.

```csharp
// Example request:
// GET http://router.project-osrm.org/route/v1/driving/-0.141588,51.501009;-2.242631,53.480759?overview=false
{
  "code": "Ok",
  "routes": [
    {
      "distance": 331450.2,
      "duration": 14210.5
    }
  ]
}
```

### Distance Calibration Factor (+7%)

In real-world driving conditions, pure mathematical route distances from OpenStreetMap can slightly underestimate actual vehicle odometer readings and commercial navigation apps (such as Google Maps) due to junction turning arcs, multi-lane routing, and local road alterations.

To provide practical, realistic mileage figures, the application applies a **+7% multiplier (`* 1.07`)** to raw OSRM meter distances:

```csharp
double meters = await GetDrivingDistanceMetersAsync(coords[start], coords[end]) * 1.07;
```

---

## Application Usage Guide

### Input Format & Syntax

#### Single Journey
Enter two or more UK postcodes separated by commas:
```text
SW1A 1AA, M1 1AE, EH1 1YZ
```
*Calculates the driving distance from Buckingham Palace (London) $\to$ Manchester Piccadilly $\to$ Edinburgh Waverley.*

#### Multiple Journeys in One Batch
Separate distinct routes using semicolons (`;`):
```text
SW1A 1AA, M1 1AE ; B1 1AA, LS1 1UR ; CB2 1TN, NR1 1ER
```
- Each journey section is calculated independently.
- The **Journey Breakdown** panel outputs individual route breakdowns followed by a combined summary.
- Clicking `+ Save to History` saves the journeys together as one dated batch in the History panel.

Postcodes can also be entered one per line instead of comma-separated.

### Date Tagging

- Located next to the postcode input box.
- **Optional**: Can be left blank, or populated with any string (e.g., `24/09`, `2026-09-24`, `Trip 1`). Useful when tracking which day each journey happened.
- The placeholder hint shows today's date (`dd/MM`) as a guide to the format.
- When provided, date tags are prefixed to journey headers (`[24/09]`) and shown alongside history items.
- Separately, every saved batch is always stamped with the date and time it was calculated. This isn't optional, so saved history can always be dated.

### History Management

- **Save to History**: After calculating, click `+ Save to History`. The input fields clear and the journeys appear in the **Saved Journeys** sidebar as one batch, headed by its date and time, journey count and total.
- **Persistence**: History is written to `history.json` in the same folder as the executable after every change. Keep the app in a folder you can write to (not `Program Files`); if saving fails, the app says so and keeps history until it closes. If the file is ever unreadable, it is renamed to `history.corrupt-<timestamp>.json` rather than overwritten.
- **Recall**: Click a batch header to load all its journeys into the breakdown, or click a single journey to load just that one.
- **Delete**: Click the red cross on a journey to remove it, or on a batch header to remove the whole batch (asks first if it holds several journeys). With the list focused, select a row and press **Delete** to do the same from the keyboard. The **History Total** recalculates automatically.
- **Clear**: Click **Clear** at the top of the panel to delete all history after confirming, e.g. once a month's total has been copied.
- **Collapse / Expand Sidebar**: Click the chevron button at the top right of the panel to collapse it for a wider breakdown view; click **History** to bring it back.

### Clipboard & Quick Actions

- **Copy Journey Total**: Click either the `Journey Total: ...` label directly or its adjacent **Copy** button.
- **Copy History Total**: Click the `History Total: ...` label directly or its adjacent **Copy** button.
- Only the number is copied (e.g. `123.4`, in the currently selected unit), so it pastes straight into a spreadsheet cell. A popup above the button and the status bar show exactly what was copied.
- **Help Dialog**: Click the top-right **Help** button (or the info icon next to "Enter Postcodes") to view quick syntax instructions.

### Keyboard Shortcuts

| Key | Where | Action |
| :--- | :--- | :--- |
| `Ctrl+Enter` | Postcode box | Calculate |
| `Enter` | Postcode box | New line (postcodes can be one per line) |
| `Enter` | Date box | Calculate |
| `Tab` | Anywhere | Moves through postcodes, date, unit, Calculate, Save, breakdown, Copy, history list, history Copy, Clear |
| `Up` / `Down` | History list | Select and load a batch or journey |
| `Delete` | History list | Remove the selected batch or journey |

---

## Project Structure

```text
PostcodeDistanceCalculator/
│
├── UI/
│   ├── PostcodeDistanceCalculator.cs          # Main form: wires the classes below to the UI, history list drawing & clicks
│   ├── PostcodeDistanceCalculator.Designer.cs # Windows Forms layout, controls, and event wiring
│   ├── PostcodeDistanceCalculator.resx        # Form XML resource definitions
│   └── IconFont.cs                            # Draws Material Symbols icons from the embedded font
├── Models/
│   ├── Leg.cs                                 # One driving leg between two postcodes
│   ├── Journey.cs                             # A route of legs; totals derived from the legs
│   ├── JourneyBatch.cs                        # Journeys saved together, stamped with when they were calculated
│   └── DistanceUnit.cs                        # Miles / kilometers enum
├── Extensions/
│   └── DistanceUnitExtensions.cs              # Unit conversion, names and number formatting
├── Helpers/
│   ├── JourneyParser.cs                       # Splits input text into journeys and normalised postcodes
│   └── BreakdownFormatter.cs                  # Builds the plain-text Journey Breakdown
├── Services/
│   ├── RouteApi.cs                            # postcodes.io and OSRM calls, calibration factor
│   └── HistoryStore.cs                        # Loads/saves history.json next to the executable
├── Resources/
│   ├── MaterialSymbolsOutlined.ttf            # Icon font subset from fonts.google.com/icons, embedded in the exe
│   └── app.ico                                # Application icon ("route" Material Symbol)
├── Program.cs                             # Application entry point with DPI awareness initialization
├── PostcodeDistanceCalculator.csproj      # .NET 10 project file: version, metadata, app icon, DPI mode
├── PostcodeDistanceCalculator.slnx        # Solution file containing the app and test projects
├── PostcodeDistanceCalculator.Tests/      # xUnit tests for the parser, formatter, models, history store and icon font
├── LICENSE                                # MIT License
└── README.md                              # Project documentation (this file)
```

Each class has XML doc comments describing its role and behaviour.

---

## Prerequisites

- **Operating System**: Windows 10 / Windows 11 (x64)
- **Runtime** (if running unbundled builds): [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/download)
- **SDK** (for building from source): [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- **Active Internet Connection**: Required at runtime for postcode geocoding and routing queries.

---

## Build and Publish Instructions

The version number lives in `PostcodeDistanceCalculator.csproj` (`<Version>`). It's stamped into the exe's file properties and shown in the Help dialog.

### Local Development & Running

To build and run the application locally from the command line:

```powershell
# Restore dependencies and build the app and tests
dotnet build

# Run the Windows Forms application
dotnet run --project PostcodeDistanceCalculator.csproj

# Run the unit tests
dotnet test
```

The tests cover input parsing, breakdown formatting, unit conversion and history saving/loading. They don't call the live APIs.

Or open `PostcodeDistanceCalculator.slnx` in Visual Studio 2026 and press `F5`.

---

### Single-File Self-Contained Executable

To generate a standalone, portable Windows `.exe` that runs on any 64-bit Windows PC without requiring .NET 10 to be installed, execute the following command in the project root:

```powershell
dotnet publish PostcodeDistanceCalculator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

#### Output Location
The generated single-file executable is created at:
```text
bin/Release/net10.0-windows/win-x64/publish/PostcodeDistanceCalculator.exe
```

---

### Publish Parameter Breakdown

| Parameter / Flag | Description |
| :--- | :--- |
| `-c Release` | Compiles the code using the **Release** configuration with full compiler optimizations and debug symbol stripping. |
| `-r win-x64` | Specifies the target **Runtime Identifier (RID)** for 64-bit Windows systems. |
| `--self-contained true` | Bundles the .NET 10 Windows Forms runtime and base class libraries directly inside the deployment package. End users do not need .NET installed on their system. |
| `-p:PublishSingleFile=true` | Packages managed assemblies, dependencies, and resources into a single standalone `.exe` binary. |
| `-p:IncludeNativeLibrariesForSelfExtract=true` | Embeds native C/C++ runtimes and operating system binaries into the single file, automatically extracting them to a temporary location or memory at runtime. |
| `-p:EnableCompressionInSingleFile=true` | Compresses embedded assemblies inside the executable bundle, significantly reducing the final file size for distribution. |

---

## Dependencies & Attribution

- **Geocoding**: Powered by [postcodes.io](https://postcodes.io/) (Open-source UK postcode and geodemographic data API based on Ordnance Survey Open Data, Royal Mail PAF, and ONS).
- **Routing Engine**: Powered by [Project OSRM](https://project-osrm.org/) (Open Source Routing Machine).
- **Map Data**: [OpenStreetMap](https://www.openstreetmap.org/) contributors under the Open Database License (ODbL).
- **Icons**: [Material Symbols](https://fonts.google.com/icons) by Google, under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0). A subset containing only the icons the app uses is stored in `Resources/` and embedded in the executable, so icons work offline and don't depend on Google Fonts being available. See `UI/IconFont.cs` for how to add icons. The application icon is the "route" symbol (Rounded, filled) on the app's colours.

---

## Disclaimer

This project was built with the assistance of [Claude Code](https://claude.com/claude-code), Anthropic's AI coding tool.
However, all decisions and designs are reviewed and implemented by the author. The AI is used only as a coding assistant not a final say.

---

## License

This project is licensed under the [MIT License](LICENSE). The bundled Material Symbols icons are under the Apache License 2.0 (see [Dependencies & Attribution](#dependencies--attribution)).

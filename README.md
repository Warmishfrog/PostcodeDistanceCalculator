# UK Postcode Distance Calculator

A lightweight, modern Windows Forms desktop application built with **.NET 10** to calculate driving distances between UK postcodes. The tool supports multi-stop routes, batch processing of multiple independent journeys, live unit conversion (miles / kilometers), interactive route breakdowns, and session history management with one-click clipboard copying.

---

## Table of Contents

- [Overview](#overview)
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
- [License](#license)

---

## Overview

Calculating driving distances across sequences of UK postcodes is a common requirement for travel planning, mileage expenses, logistics, and delivery estimates. 

**UK Postcode Distance Calculator** provides a clean, single-window graphical interface that:
1. Translates arbitrary UK postcodes into precise geographical coordinates using the free [postcodes.io](https://postcodes.io/) REST API.
2. Calculates real driving routes between consecutive waypoints using the [Project OSRM](https://project-osrm.org/) routing engine.
3. Automatically adjusts raw routing distances with a calibrated multiplier to closely reflect real-world navigation systems (e.g. Google Maps).
4. Maintains an active session history sidebar allowing users to compare routes, track cumulative distances, and export totals to the clipboard.

---

## Key Features

- **Multi-Leg Route Calculation**: Enter 2 or more UK postcodes to compute cumulative driving distance leg-by-leg.
- **Batch Journey Processing**: Separate multiple independent journeys using semicolons (`;`) to calculate them all in a single operation.
- **Dynamic Unit Switching**: Instantly switch between **Miles** and **Kilometers (km)** with automatic, live conversion of current results and saved history without re-querying the APIs.
- **Collapsible Saved Journeys Sidebar**:
  - Save completed calculations to the session history list.
  - Owner-drawn list items showing custom date tags, initial postcode, and total distance.
  - Individual item removal with inline delete buttons (`✕`).
  - Running grand total across all saved journeys.
  - Collapsible panel with toggle button to maximize screen space.
- **One-Click Clipboard Export**: Click either total label or dedicated `📋 Copy` buttons to copy figures for spreadsheets or expense forms.
- **Date Tagging with Win32 Cue Banner**: Optional date field equipped with native Windows gray placeholder hint (`dd/MM`).
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
   - Coordinates are queried asynchronously and stored in a temporary dictionary `coords[code] = (Lat, Lon)` so repeated postcodes are only fetched once.

3. **Sequential Leg Routing**:
   - The app loops through postcodes `0` to `N - 1`, pairing adjacent postcodes (`postcodes[i]` $\to$ `postcodes[i + 1]`).
   - Each leg is sent to OSRM to determine the driving distance in meters.

4. **Formatting & Display**:
   - Legs are formatted into a structured ASCII/Unicode breakdown table showing leg numbers, start/end postcodes, individual leg distances, and overall totals.
   - The UI updates the `Journey Total` label, activates the `+ Save to History` button, and logs status messages.

---

## External API Integrations

The application communicates with two public REST APIs asynchronously using a shared `HttpClient` instance.

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
- **Request Headers**: Custom `User-Agent: WinFormsPostcodeDistanceTool` header included to comply with standard routing API usage policies.
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
- Clicking `+ Save to History` saves all batch journeys as separate entries in the History panel.

### Date Tagging

- Located next to the postcode input box.
- **Optional**: Can be left blank, or populated with any string (e.g., `24/09`, `2026-09-24`, `Trip 1`).
- The text box displays a native Windows gray placeholder (cue banner) indicating today's date (`dd/MM`).
- When provided, date tags are prefixed to journey headers (`[24/09]`) and shown alongside history items.

### History Management

- **Save to History**: After calculating a route, click `+ Save to History`. The input fields will clear, and the calculated routes will appear in the **Saved Journeys** sidebar.
- **Recall Journey**: Click any saved journey row to restore its postcodes, date, and full breakdown in the main panel.
- **Delete Entry**: Click the red **✕** icon on any history item to remove it. The **History Total** automatically recalculates.
- **Collapse / Expand Sidebar**: Click `▶` to collapse the history pane for a wider breakdown view; click `📜 History ◀` to bring it back.

### Clipboard & Quick Actions

- **Copy Journey Total**: Click either the `Journey Total: ...` label directly or its adjacent `📋 Copy` button.
- **Copy History Total**: Click the `History Total: ...` label directly or its adjacent `📋 Copy` button.
- **Help Dialog**: Click the top-right `❓ Help` button to view quick syntax instructions.

---

## Project Structure

```text
PostcodeDistanceCalculator/
│
├── PostcodeDistanceCalculator.cs          # Main form logic, async API calls, history & state handling
├── PostcodeDistanceCalculator.Designer.cs # Windows Forms layout, controls, and event wiring
├── PostcodeDistanceCalculator.resx        # Form XML resource definitions
├── Program.cs                             # Application entry point with DPI awareness initialization
├── PostcodeDistanceCalculator.csproj      # .NET 10 project file (WinExe, Nullable, Windows Forms enabled)
├── PostcodeDistanceCalculator.slnx        # Modern XML solution file (.NET 10 / Visual Studio 2026)
└── README.md                              # Project documentation (this file)
```

---

## Prerequisites

- **Operating System**: Windows 10 / Windows 11 (x64)
- **Runtime** (if running unbundled builds): [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/download)
- **SDK** (for building from source): [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- **Active Internet Connection**: Required at runtime for postcode geocoding and routing queries.

---

## Build and Publish Instructions

### Local Development & Running

To build and run the application locally from the command line:

```powershell
# Restore dependencies and build
dotnet build

# Run the Windows Forms application
dotnet run
```

Or open `PostcodeDistanceCalculator.slnx` in Visual Studio 2026 / Visual Studio Code and press `F5`.

---

### Single-File Self-Contained Executable

To generate a standalone, portable Windows `.exe` that runs on any 64-bit Windows PC without requiring .NET 10 to be installed, execute the following command in the project root:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
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

---

## License

This project is licensed under the [MIT License](LICENSE) (or your preferred repository license). Feel free to use, modify, and distribute as needed.

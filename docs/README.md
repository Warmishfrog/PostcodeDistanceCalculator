# UK Postcode Distance Calculator

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
  - [How Accurate Are the Distances?](#how-accurate-are-the-distances)
- [Application Usage Guide](#application-usage-guide)
  - [Input Format & Syntax](#input-format--syntax)
  - [Date Tagging](#date-tagging)
  - [Cancelling a Calculation](#cancelling-a-calculation)
  - [Postcodes to Check (Typo Warnings)](#postcodes-to-check-typo-warnings)
  - [History Management](#history-management)
  - [Verifying Distances with Google Maps](#verifying-distances-with-google-maps)
  - [Clipboard & Quick Actions](#clipboard--quick-actions)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Build and Publish Instructions](#build-and-publish-instructions)
  - [Local Development & Running](#local-development--running)
  - [Single-File Self-Contained Executable](#single-file-self-contained-executable)
  - [Publish Parameter Breakdown](#publish-parameter-breakdown)
- [Dependencies & Attribution](#dependencies--attribution)
- [Disclaimer](#disclaimer)
- [Changelog](#changelog)
- [License](#license)

---

## Overview

Calculating driving distances across sequences of UK postcodes is a common requirement for travel planning, mileage expenses, logistics, and delivery estimates. 

**UK Postcode Distance Calculator** provides a clean, single-window graphical interface that:
1. Translates arbitrary UK postcodes into precise geographical coordinates using the free [postcodes.io](https://postcodes.io/) REST API.
2. Calculates real driving routes between consecutive waypoints using the [Project OSRM](https://project-osrm.org/) routing engine.
3. Reports the routing service's driving distances as they are, with an optional check against Google Maps.
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
- **Google Maps Verification**: Open any journey in Google Maps with one click and enter Google's distance; it replaces the app's estimate in totals, copies and history.
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
|     POST /postcodes (all postcodes in one call)      |
+--------------------------+---------------------------+
                           |
                           v
+------------------------------------------------------+
|          Routing Engine (Project OSRM)               |
|  GET /route/v1/driving/{stop};{stop};... per journey |
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

2. **Batched Geocoding**:
   - Every unique postcode across all journeys in the batch is collected.
   - They are looked up together in one bulk postcodes.io request (up to 100 per request), so a postcode that appears in several journeys is only fetched once.
   - Postcodes that aren't found are retried against postcodes.io's list of retired postcodes; if found there, their last known location is used and the user is told.

3. **One Routing Request per Journey**:
   - All of a journey's stops are sent to OSRM in a single request, which returns the driving distance for every leg (`postcodes[i]` to `postcodes[i + 1]`).
   - This gives exactly the same leg distances as routing each leg separately, with far fewer requests.

4. **Formatting & Display**:
   - Legs are formatted into a structured ASCII/Unicode breakdown table showing leg numbers, start/end postcodes, individual leg distances, and overall totals.
   - The UI updates the `Journey Total` label, activates the `+ Save to History` button, and logs status messages.

---

## External API Integrations

The application communicates with two public REST APIs over HTTPS using a shared `HttpClient` instance (15 second timeout, `User-Agent: WinFormsPostcodeDistanceTool` on every request). Requests are awaited, so the window stays responsive while they run. A batch needs one postcode lookup plus one routing request per journey.

### 1. Geocoding via postcodes.io

- **Endpoint**: `https://api.postcodes.io/postcodes` (bulk lookup)
- **Method**: `POST` with `{"postcodes": ["SW1A1AA", "M11AE", ...]}`, up to 100 postcodes per request
- **Purpose**: Retrieves WGS84 latitude and longitude for every postcode in the batch at once.
- **Response Format**: One entry per postcode with the original `query` and a `result` that is either the postcode's details or `null` if it isn't a current postcode.
- **Retired postcodes**: Postcodes with a `null` result are retried at `GET https://api.postcodes.io/terminated_postcodes/{postcode}`, which still gives a last known location and the year it was retired.
- **Error Handling**:
  - All unknown postcodes are reported together in one message, rather than stopping at the first.
  - Valid postcodes with no map location (e.g. PO boxes) are reported as unroutable.
  - Network errors, timeouts, HTTP errors and malformed responses become a readable error message.

```csharp
// Example request:
// POST https://api.postcodes.io/postcodes   {"postcodes":["SW1A1AA","ZZ99ZZ"]}
{
  "status": 200,
  "result": [
    { "query": "SW1A1AA", "result": { "postcode": "SW1A 1AA", "latitude": 51.50101, "longitude": -0.141563 } },
    { "query": "ZZ99ZZ", "result": null }
  ]
}
```

### 2. Driving Route Calculation via Project OSRM

- **Endpoint**: `https://router.project-osrm.org/route/v1/driving/{lon},{lat};{lon},{lat};...?overview=false`
- **Method**: `GET`, one request per journey with every stop in order
- **Purpose**: Computes the fastest driving route through the stops on OpenStreetMap road data, with the distance of each leg.
- **Usage limits**: `router.project-osrm.org` is OSRM's public demo server. Its [usage policy](https://github.com/Project-OSRM/osrm-backend/wiki/Demo-server) allows at most one request per second and it isn't meant for production use, so heavy use may be rate-limited or blocked. The app waits at least one second between routing requests to stay within the limit, so a batch takes roughly a second per journey. That's fine for personal mileage tracking; anything larger should run its own OSRM server.
- **Formatting**: Coordinates are formatted with `CultureInfo.InvariantCulture` so decimal points are always `.`, whatever the Windows locale.
- **Response Format**: JSON with a `code` (`"Ok"`, `"NoRoute"`, ...) and `routes[0].legs[i].distance` in metres for each leg.
- **Error Handling**:
  - `"NoRoute"` is reported as no drivable route between the postcodes.
  - The number of legs returned is checked against the number of stops.

```csharp
// Example request:
// GET https://router.project-osrm.org/route/v1/driving/-0.141563,51.50101;-2.231182,53.483487;-3.193018,55.950328?overview=false
{
  "code": "Ok",
  "routes": [
    {
      "distance": 664358.8,
      "legs": [
        { "distance": 320453.1, "duration": 14664.2 },
        { "distance": 343905.7, "duration": 15744.6 }
      ]
    }
  ]
}
```

### How Accurate Are the Distances?

Distances are OSRM's driving distances from OpenStreetMap, shown as they are. (Versions 2.1.0 and earlier added 7% to every distance to get closer to Google Maps; that was removed because one percentage can't fit short local trips and long motorway trips equally well. History saved by those versions keeps its +7% figures.)

Google Maps can differ, by more on some trips than others: it picks routes using live traffic, uses its own map data, and may start from a full address rather than the postcode's centre. When a figure needs to match Google exactly (e.g. for a mileage claim), use [Verifying Distances with Google Maps](#verifying-distances-with-google-maps).

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

#### Dated Journeys, One Per Line
Start a line with a date label and a colon, and that line starts a new journey tagged with that date:
```text
21st: BS15 8DB, BS5 8NG, BS4 3LF, BS5 6SX, BS15 8DB
22nd: BS15 8DB, BS15 8BE, BS4 4PR, BS15 9TR, BS10 7LD, BS15 8DB
```
- Any label works (`21st`, `21/10`, `Mon`), up to 20 characters without commas.
- This works alongside the other formats: lines without a label carry on the journey above them (one postcode per line still works), semicolons still separate journeys, and journeys without a label use the **Date** box.
- Stray double commas are ignored, and postcodes missing a comma between them (`BS14 0SL BS15 8DB`) are split apart.

### Date Tagging

- Located next to the postcode input box.
- **Optional**: Can be left blank, or populated with any string (e.g., `24/09`, `2026-09-24`, `Trip 1`). Useful when tracking which day each journey happened.
- The placeholder hint shows today's date (`dd/MM`) as a guide to the format.
- When provided, date tags are prefixed to journey headers (`[24/09]`) and shown alongside history items.
- Separately, every saved batch is always stamped with the date and time it was calculated. This isn't optional, so saved history can always be dated.

### Cancelling a Calculation

While a calculation runs, **Calculate Route** becomes **Cancel**. Cancelling stops the lookups and puts the results, the Save button and the status back exactly as they were before you clicked Calculate. Your typed postcodes are left as you typed them.

### Postcodes to Check (Typo Warnings)

Postcodes that may be typos get a wavy amber underline in the postcode box, the breakdown and the history list, and an amber **postcodes to check** link appears beside **Enter Postcodes**. Hover over an underline (or a history row) to see why; click the link for the full list. These are only warnings: a flagged postcode still calculates if it exists.

- **Not a postcode**: text that isn't in the UK postcode format, or ends in letters UK postcodes never use (C, I, K, M, O, V).
- **Possible typo**: one slip away from a postcode you use at least twice as often (in the box and in history): one character swapped for a neighbouring key or a look-alike (S/D, O/0), or two neighbouring characters swapped round. For example `BS15 8SB` among many `BS15 8DB`s. Real neighbours a key apart, like `BS14 0SL` and `BS14 0SU`, aren't flagged.

### History Management

- **Save to History**: After calculating, click `+ Save to History`. The input fields clear and the journeys appear in the **History** sidebar as one batch, headed by its date and time, journey count and total.
- **Persistence**: History is written to `history.json` in the same folder as the executable after every change. Keep the app in a folder you can write to (not `Program Files`); if saving fails, the app says so and keeps history until it closes. If the file is ever unreadable, it is renamed to `history.corrupt-<timestamp>.json` rather than overwritten.
- **Recall**: Click a batch header to load all its journeys into the breakdown, or click a single journey to load just that one.
- **Delete**: Click the red cross on a journey to remove it, or on a batch header to remove the whole batch (asks first if it holds several journeys). With the list focused, select a row and press **Delete** to do the same from the keyboard. The **History Total** recalculates automatically.
- **Export**: Click **Export** to save all history to a `.txt` file wherever you choose (the file dialog starts in the app's folder). The top of the file is a readable report of every batch, journey and total; below a marked line is the exact data used for importing.
- **Import**: Click **Import** and pick an exported `.txt` file to add its batches to your history, e.g. to bring back a past month. Batches already in history are skipped, so importing the same file twice doesn't duplicate anything.
- **Clear**: Click **Clear** at the top of the panel to delete all history after confirming, e.g. once a month's total has been copied (export first to keep a copy).
- **Collapse / Expand Sidebar**: Click the chevron button at the top right of the panel to collapse it for a wider breakdown view; click **History** to bring it back.
- **Collapse batches**: Click the chevron at the left of a batch header (or select it and press Left / Right) to hide or show its journeys.
- **Select several**: Ctrl+click or Shift+click rows to select more than one, then right-click to verify them all with Google Maps, use the app's estimate for all of them, or press **Delete** to remove them together. A plain click still loads a row.
- **Resize the boxes**: Drag the dotted bars between the postcode box, the breakdown and the history panel to make any of them bigger or smaller. Journey rows in history show the full route, so a wider panel shows more of it.

### Verifying Distances with Google Maps

The app's distances are estimates from OpenStreetMap routing. To check a journey against Google Maps, or to use Google's figure instead:

1. **For the current results**, click **Verify** next to **Copy**. **For saved history**, right-click a journey (or a batch header, to go through all its journeys) and choose **Verify with Google Maps...**.
2. Google Maps opens in your browser with the journey's postcodes filled in as driving directions (no account or API key needed).
3. A small window asks for the distance Google shows. Type or paste it (e.g. `210.4` or `210.4 mi`) in the unit the app is showing, then **Save**. **Skip** leaves that journey as it is. When checking several journeys, **Stop** ends there and keeps the figures saved so far, while **Cancel** (or Esc) ends and discards every figure from that run, leaving everything as it was.
4. The Google figure replaces the app's estimate for that journey's total, the batch and history totals, and copied numbers. The breakdown shows the Google figure with the app's estimate beneath it, and history rows are marked "Google Maps".

To go back to the estimate, right-click the journey or batch and choose **Use This App's Estimate**. **Open in Google Maps** on a saved journey just opens the route without asking for a figure.

Google Maps links support up to 11 stops (9 between the start and the end). Leg distances in the breakdown are always the app's estimates; only the journey total is replaced.

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
│   ├── IconFont.cs                            # Draws Material Symbols icons from the embedded font
│   └── VerifyDistanceDialog.cs                # Asks for Google's distance while Google Maps is open
├── Models/
│   ├── Leg.cs                                 # One driving leg between two postcodes
│   ├── Journey.cs                             # A route of legs; totals derived from the legs
│   ├── JourneyBatch.cs                        # Journeys saved together, stamped with when they were calculated
│   ├── DistanceUnit.cs                        # Miles / kilometers enum
│   └── PostcodeLocation.cs                    # Latitude/longitude from postcodes.io (flags retired postcodes)
├── Extensions/
│   └── DistanceUnitExtensions.cs              # Unit conversion, names and number formatting
├── Helpers/
│   ├── JourneyParser.cs                       # Splits input text into journeys and normalised postcodes
│   ├── GoogleMapsLink.cs                      # Builds Google Maps directions links for verifying journeys
│   ├── DistanceInput.cs                       # Reads distances typed or pasted from Google Maps
│   └── BreakdownFormatter.cs                  # Builds the plain-text Journey Breakdown
├── Services/
│   ├── RouteApi.cs                            # Bulk postcode lookup and per-journey OSRM routing, cancellable
│   ├── RouteLookupException.cs                # Expected lookup/routing failures with user-readable messages
│   ├── RequestThrottle.cs                     # Keeps OSRM requests at least a second apart
│   └── HistoryStore.cs                        # Loads/saves history.json next to the executable
├── Resources/
│   ├── MaterialSymbolsOutlined.ttf            # Icon font subset from fonts.google.com/icons, embedded in the exe
│   └── app.ico                                # Application icon ("route" Material Symbol)
├── Program.cs                             # Application entry point with DPI awareness initialization
├── PostcodeDistanceCalculator.csproj      # .NET 10 project file: version, metadata, app icon, DPI mode
├── PostcodeDistanceCalculator.slnx        # Solution file containing the app and test projects
├── PostcodeDistanceCalculator.Tests/      # xUnit tests for the parser, formatter, models, history store and icon font
├── docs/
│   ├── README.md                              # Project documentation (this file)
│   └── CHANGELOG.md                           # Changes in each version
├── CLAUDE.md                              # Local Claude Code instructions (not tracked; must stay at the root)
└── LICENSE                                # MIT License
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

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for what changed in each version.

---

## License

This project is licensed under the [MIT License](../LICENSE). The bundled Material Symbols icons are under the Apache License 2.0 (see [Dependencies & Attribution](#dependencies--attribution)).

# UK Postcode Distance Calculator

A small Windows app that works out driving distances between UK postcodes. Type in a route, or a month of trips, and it gives each journey's distance and a running total you can copy into a mileage claim or spreadsheet.

<img width="944" height="603" alt="UkPostcodeDistanceCalculator" src="https://github.com/user-attachments/assets/af25fc1f-2530-455d-bef3-2aae538715de" />

**[Download the latest version](https://github.com/Warmishfrog/PostcodeDistanceCalculator/releases/latest)** | [Changelog](Documentation/CHANGELOG.md) | [Developer guide](Documentation/DEVELOPER.md)

- [Download](#download)
- [Quick start](#quick-start)
- [Entering postcodes](#entering-postcodes)
- [Settings](#settings)
- [History](#history)
- [Checking against Google Maps](#checking-against-google-maps)
- [How accurate is it?](#how-accurate-is-it)
- [More information](#more-information)

## Download

1. Download **PostcodeDistanceCalculator.exe** from the [latest release](https://github.com/Warmishfrog/PostcodeDistanceCalculator/releases/latest).
2. Put it in a folder of its own that you can write to, e.g. `Documents\Postcode Distance Calculator` (not `Program Files`). Your history and settings are saved next to it.
3. Double-click it. There's nothing to install.

Needs Windows 10 or 11 (64-bit) and an internet connection. The first time you run it, Windows may say "Windows protected your PC" because the exe isn't code-signed: click **More info**, then **Run anyway**.

**To upgrade**, replace the old exe with the new one in the same folder. Your history carries over.

## Quick start

Adding up a month's mileage:

1. Type each trip on its own line, starting with the date:
   ```text
   21st: BS15 8DB, BS5 8NG, BS4 3LF, BS15 8DB
   22nd: BS15 8DB, BS4 4PR, BS15 8DB
   ```
2. Click **Calculate Route** (or press Ctrl+Enter), check the breakdown, then click **+ Save to History**.
3. Repeat through the month.
4. At month end, click **Copy** next to **History Total**. Only the number is copied (e.g. `412.6`), ready to paste.
5. Click **Export** to keep a copy, then **Clear** to start the next month.

## Entering postcodes

| You type | You get |
| :--- | :--- |
| `SW1A 1AA, M1 1AE, EH1 1YZ` | One journey through those stops in order |
| `SW1A 1AA, M1 1AE ; B1 1AA, LS1 1UR` | Two journeys (semicolons separate them) |
| `21st: BS15 8DB, BS5 8NG` on each line | One journey per line, tagged with that date |
| One postcode per line | One journey |

- Any date label works (`21st`, `21/10`, `Mon`). Journeys without one use the **Date** box, which is optional.
- Capitals and spaces don't matter, and postcodes missing a comma between them are split apart.
- Postcodes that look like typos get an amber wavy underline. Hover over it to see why. They're only warnings: a real postcode still calculates.
- While it's calculating, **Calculate Route** becomes **Cancel**.

## Settings

All settings are in the window and are remembered between runs:

- **Miles / Km**: switches every distance shown, including history.
- **Compact**: shows each journey's route on one line, e.g. `BS158DB  ->  BS58NG (0.7 mi), BS58NG  ->  BS43LF (3.4 mi)`, which is easy to copy.
- **Leg distances**: shows or hides the distance of each leg.
- **Estimate vs Google**: on journeys checked against Google Maps, also shows this app's own figure.

You can also drag the dotted bars between the boxes to resize them, and the chevron at the top right hides the history panel. These aren't remembered.

Files kept next to the exe:

| File | Holds |
| :--- | :--- |
| `history.json` | Your saved history |
| `settings.json` | The settings above |

## History

- **+ Save to History** saves the current journeys as one batch, stamped with the date and time.
- Click a batch header to load all its journeys, or a journey to load just that one.
- Click the red cross on a row, or select it and press **Delete**, to remove it.
- Ctrl+click or Shift+click to select several rows, then right-click for options.
- **Export** saves everything to a readable `.txt` file. **Import** adds an exported file back in, skipping anything already there.
- **Clear** deletes all history (after asking).

## Checking against Google Maps

When a figure must match Google Maps (e.g. for a mileage claim):

1. Click **Verify in Google Maps** under the breakdown, or right-click a saved journey or batch and choose **Verify with Google Maps...**.
2. Google Maps opens in your browser with the route filled in.
3. Type the distance Google shows (e.g. `210.4`) and click **Save**.

Google's figure then replaces the app's estimate in totals, history and copied numbers. To undo, right-click the journey and choose **Use This App's Estimate**. Google's distances can change with traffic, roadworks or closures, so the same route may give a different figure on another day. Google Maps links allow up to 11 stops.

## How accurate is it?

Distances come from [OSRM](https://project-osrm.org/) driving routes on [OpenStreetMap](https://www.openstreetmap.org/) roads, from the centre of each postcode. They're usually close to Google Maps but not identical, since Google uses its own map data, live traffic and exact addresses. Use [Checking against Google Maps](#checking-against-google-maps) when it needs to match exactly.

Versions 2.1.0 and earlier added 7% to every distance. 2.2.0 doesn't; history saved by older versions keeps its figures.

## More information

- [Developer guide](Documentation/DEVELOPER.md): how it works, the APIs it calls, building from source.
- [Changelog](Documentation/CHANGELOG.md): what changed in each version.
- Click **Help** in the app for a quick reference.

Postcode locations come from [postcodes.io](https://postcodes.io/), routes from [Project OSRM](https://project-osrm.org/), and map data from [OpenStreetMap](https://www.openstreetmap.org/) contributors (ODbL). Icons are [Material Symbols](https://fonts.google.com/icons) by Google (Apache License 2.0).

Built with the help of [Claude Code](https://claude.com/claude-code); all decisions and designs are reviewed by the author.

Licensed under the [MIT License](LICENSE).

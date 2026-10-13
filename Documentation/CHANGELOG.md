# Changelog

All notable changes to UK Postcode Distance Calculator are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html). The version number lives in `PostcodeDistanceCalculator.csproj` (`<Version>`).

## [Unreleased]

### Added
- **Postcodes to check.** Postcodes that may be typos get a wavy amber underline in the postcode box, the breakdown and history, with the reason on hover and a "postcodes to check" link listing them all. Flags text that isn't a UK postcode, and postcodes one key slip away from one you use much more often (e.g. `BS15 8SB` among many `BS15 8DB`s).
- **Cancel a calculation.** While calculating, the Calculate button becomes Cancel, which stops the lookups and restores the previous results.
- **Cancel verifying.** The Google Maps verification window has a Cancel button (and Esc) that discards every figure entered in that run. Stop still ends early and keeps them.
- **Select several history rows** with Ctrl+click or Shift+click, then right-click to verify or reset them all, or press Delete to remove them.
- **Collapsible batches** in history: click a batch's chevron, or press Left / Right.
- The Verify button reads "Verify in Google Maps (3)", showing how many journeys it will check, and its tooltip explains it opens Google Maps in your browser.
- **Breakdown options** above the Journey Breakdown: **Compact** puts each journey's legs on one line (`BS15 8DB  ->  BS5 8NG (0.7 mi), BS5 8NG  ->  BS4 3LF (3.4 mi)`), **Leg distances** shows or hides each leg's distance, and **Estimate vs Google** shows or hides this app's estimate beside Google's figure. These choices and Miles/Km are remembered in `settings.json` next to the app.
- A note that Google Maps distances can change with traffic, roadworks or closures, under verified totals and in the verify window.
- **Dated journeys.** A line starting with a date and a colon (`21st: BS15 8DB, BS5 8NG, BS15 8DB`) starts a new journey tagged with that date, so a month of dated trips can be pasted in one go. Works alongside commas, semicolons and one postcode per line.
- The postcode box shows example formats while it is empty.
- **Export and Import history.** Export saves all history to a `.txt` file in a folder of your choice, readable as a report; Import adds an exported file back in, skipping batches already there.

### Changed
- **Distances no longer have 7% added.** They're the routing service's driving distances as they are. History saved by earlier versions keeps its figures.
- After calculating, and when loading from history, the postcode box is tidied to one journey per line with a blank line between, postcodes spaced and capitalised (`BS15 8DB, BS5 8NG`). Saved journeys with different dates are loaded with their date labels, so recalculating keeps each date.
- The history panel heading is now "History", to make room for the Import and Export buttons.
- The bars between the postcode box, the breakdown and the history panel are now visible, with grab handles, and slightly wider to grab. Dragging them resizes the boxes, within limits that keep every control usable.
- History journey rows show the full route (`BS15 8DB -> BS5 8NG -> ...`) with the distance on the right, so widening the panel reveals more stops.

### Fixed
- Two postcodes with no comma between them (`BS14 0SL BS15 8DB`) are split apart instead of failing as one unknown postcode.

## [2.1.0] - 2026-10-13

### Added
- **Verify with Google Maps.** A **Verify** button (current results) and a right-click menu (saved journeys and batches) open each journey in Google Maps, then ask for Google's distance. The Google figure replaces the app's estimate in totals, copied numbers and history; **Use This App's Estimate** undoes it. Uses free Google Maps links, so no account or API key is needed. ([#5](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/5))
- Retired postcodes fall back to their last known location (via postcodes.io's retired postcode list), and the user is told which ones and the year they were retired. ([#5](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/5))

### Changed
- Far fewer API requests: every postcode in a batch is looked up in one bulk request, and each journey is routed in one request covering all its stops instead of one per leg. Leg distances are unchanged. ([#5](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/5))
- The routing service is called over HTTPS. ([#5](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/5))
- All unknown postcodes are reported together in one message instead of stopping at the first. Unexpected errors are reported as such, separately from lookup failures. ([#5](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/5))
- Documentation moved into the `docs` folder, and this changelog added. ([#6](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/6))

## [2.0.0] - 2026-10-13

### Added
- **Saved history.** History is saved to `history.json` next to the executable (portable, no AppData) after every change, and grouped into batches stamped with the date and time they were calculated. Delete single journeys or whole batches, or **Clear** everything at month end. ([#2](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/2))
- Postcodes can be entered one per line as well as comma-separated. ([#1](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/1))
- Keyboard support: full tab order, Ctrl+Enter to calculate from the postcode box, Enter to calculate from the date box, Delete to remove a history row. ([#3](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/3))
- Screen reader names for icon-only controls and readable text for history rows. ([#3](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/3))
- Application icon, version and product details on the executable; version shown in the Help dialog. ([#4](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/4))
- MIT `LICENSE`. ([#4](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/4))
- Unit tests (xUnit) for parsing, formatting, units, history storage, icons and request spacing. ([#2](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/2), [#3](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/3), [#4](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/4))

### Changed
- Copy buttons and total labels copy just the number (e.g. `123.4`), with a popup showing what was copied. ([#2](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/2))
- Emoji in buttons replaced with Material Symbols icons from Google Fonts, embedded in the executable so they work offline. ([#3](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/3))
- Per-monitor DPI awareness: icons, history rows and layout scale correctly at 125% to 200% and between monitors; high contrast themes are respected. ([#3](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/3))
- Routing requests are spaced at least one second apart to stay within the OSRM demo server's usage policy. ([#4](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/4))
- Multi-journey totals are labelled "Combined Total". ([#1](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/1))
- Code split into documented Models, Extensions, Helpers, Services and UI folders. ([#2](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/2))

### Fixed
- A failed calculation could leave partial results that could be saved to history. ([#1](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/1))
- Input and history could be changed while a calculation was running. ([#1](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/1))
- Postcodes with tabs, non-breaking spaces or line breaks failed to look up; upper-casing depended on the PC's language. ([#1](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/1))
- Clicking a history row's delete button loaded it into the inputs first; double-clicking it deleted two rows. ([#1](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/1))
- Loading a history entry and saving again created duplicates. ([#1](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/1))
- Routes with fewer than two postcodes were silently dropped; they're now reported. ([#1](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/1))
- The app could crash when the clipboard was busy, or show a confusing error for postcodes with no map location. ([#1](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/1))
- An unreadable or damaged history file is set aside instead of crashing the app or being overwritten. ([#2](https://github.com/Warmishfrog/PostcodeDistanceCalculator/pull/2))

## [1.0] - 2026-09-18

First release: calculate driving distances between UK postcodes using postcodes.io and Project OSRM, with multi-stop routes, several journeys at once (separated by `;`), miles/kilometres, an optional date tag, an in-session history list and copy buttons.

[Unreleased]: https://github.com/Warmishfrog/PostcodeDistanceCalculator/compare/2.1.0...HEAD
[2.1.0]: https://github.com/Warmishfrog/PostcodeDistanceCalculator/compare/2.0.0...2.1.0
[2.0.0]: https://github.com/Warmishfrog/PostcodeDistanceCalculator/compare/1.0...2.0.0
[1.0]: https://github.com/Warmishfrog/PostcodeDistanceCalculator/releases/tag/1.0

# Changelog

All notable changes to UK Postcode Distance Calculator are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html). The version number lives in `PostcodeDistanceCalculator.csproj` (`<Version>`).

## [Unreleased]

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

# Repository Guidelines

## Project Structure & Module Organization
- `NinjaTraderAddOnProject.sln` at repo root.
- Main project in `NinjaTraderAddOnProject/`.
- Core code: `AddOnFramework.cs`, `AddOnPage.xaml`, and `AddOnPage.xaml.cs`.
- `Properties/` holds assembly metadata, resources, and settings; `App.config` stores config.
- `bin/` and `obj/` are build outputs; do not edit or commit.

## Build, Test, and Development Commands
This is a .NET Framework 4.8 project that references a local NinjaTrader 8 install.
- Visual Studio: open `NinjaTraderAddOnProject.sln` and Build.
- MSBuild (Developer PowerShell):
  - `msbuild .\NinjaTraderAddOnProject.sln /p:Configuration=Debug`
  - `msbuild .\NinjaTraderAddOnProject.sln /p:Configuration=Release`
The post-build event copies the DLL and PDB to `Documents\NinjaTrader 8\bin\Custom`. If you build outside Visual Studio, copy `NinjaTraderAddOnProject.dll` manually.

## Coding Style & Naming Conventions
- Use Allman braces, 4-space indentation, and PascalCase for public types and members.
- Private fields use camelCase (example: `addOnFrameworkMenuItem`).
- Keep namespaces under `NinjaTraderAddOnProject` or the NinjaTrader GUI namespaces already in use.
- XAML should keep consistent indentation and descriptive control names.

## Testing Guidelines
- No automated tests are present in this repository.
- Manual validation: build the DLL, launch NinjaTrader 8, enable `Tools > Options > General > Miscellaneous > Allow custom assembly loading`, then open `New > Custom Tab 1` or `New > Custom Tab 2` and verify behavior.

## Commit & Pull Request Guidelines
- History uses short, direct commit messages; keep them concise and imperative.
- PRs should include a summary, testing notes, and NinjaTrader version. Add screenshots or gifs for UI or XAML changes and mention any required configuration steps.

## Security & Configuration Tips
- References point to `Program Files (x86)\NinjaTrader 8\bin`; confirm that path exists on your machine.
- Avoid committing user-specific output from `Documents\NinjaTrader 8\bin\Custom`.

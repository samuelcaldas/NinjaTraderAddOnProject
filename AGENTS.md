# Repository Guidelines

## Project Structure & Module Organization
- `AddOnFramework.cs`: main AddOn entry, window factory, and tab logic for NinjaTrader.
- `AddOnFrameworkTab.xaml`: WPF layout for the tab UI.
- `Info.xml`: NinjaTrader export/metadata file.
- `res/`: assets (for example, `res/addonframeworkexample.png`).
- No dedicated `tests/` directory is present.

## Build, Test, and Development Commands
This repo does not ship build scripts. Use Visual Studio to compile a .NET Framework 4.8 class library referencing NinjaTrader assemblies in `C:\Program Files (x86)\NinjaTrader 8\bin\`.
- Build: Visual Studio `Build > Build Solution` (Ctrl+Shift+B).
- Deploy: copy the resulting `NinjaTraderAddOnProject.dll` to `Documents\NinjaTrader 8\bin\Custom\AddOns`.
- Run: in NinjaTrader 8, enable custom assembly loading and open `New > AddOn Framework`.

## Coding Style & Naming Conventions
- Indentation uses tabs; keep braces on their own line to match `AddOnFramework.cs`.
- Use PascalCase for types and public members (for example, `AddOnFrameworkWindow`).
- Use camelCase for locals and private fields.
- Keep file names aligned with class or view names (for example, `AddOnFrameworkTab.xaml`).
- Prefer short, purposeful comments and avoid inline noise.

## Testing Guidelines
- No automated tests are included. Perform a manual smoke test in NinjaTrader 8.
- Launch the AddOn, open both tabs, click buttons, and verify output in the UI and Log tab.
- Confirm there are no exceptions on startup or shutdown.

## Commit & Pull Request Guidelines
- Commit messages in history are short and imperative (for example, "Add screenshot", "Update README.md"); follow this style.
- Pull requests should include a concise description, testing notes, and screenshots for UI changes.
- Note the NinjaTrader version used for verification.

## Configuration Notes
- Target .NET Framework 4.8 and reference NinjaTrader DLLs locally; do not commit vendor binaries.
- Keep `Info.xml` updated when changing metadata or export behavior.

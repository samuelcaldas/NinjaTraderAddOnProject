# NinjaTrader 8 AddOn Development Instructions

## Project Context
This is a C# AddOn for NinjaTrader 8, targeting .NET Framework 4.8. It uses the NinjaTrader API (`NinjaTrader.Core`, `NinjaTrader.Gui`) and WPF for the user interface.

## Architecture & Key Components
- **Entry Point:** `AddOnFramework` (inherits `AddOnBase`) manages the AddOn lifecycle and menu integration.
- **Window Factory:** `AddOnFrameworkWindowFactory` (implements `INTTabFactory`) creates the parent window and tab pages.
- **UI Logic:** `AddOnPage` (WPF UserControl) contains the business logic, implementing `IInstrumentProvider` and `IIntervalProvider`.
- **Service Boundaries:**
  - `NinjaTrader.Cbi`: Trading objects (Orders, Accounts, Instruments).
  - `NinjaTrader.Data`: Market data streams (Bars, MarketDepth).
  - `NinjaTrader.Gui`: UI components and threading.

## Critical Workflows
- **Build:** Standard MSBuild/Visual Studio build.
- **Deployment:**
  1. Build the project.
  2. Copy the output DLL (`NinjaTraderAddOnProject.dll`) to `[User Documents]\NinjaTrader 8\bin\Custom\AddOns`.
  3. Restart NinjaTrader 8.
- **Debugging:** Attach Visual Studio debugger to the `NinjaTrader.exe` process.

## NinjaTrader Specific Patterns
- **Threading:** ALWAYS use `Core.Globals.RandomDispatcher.BeginInvoke` when updating UI from background threads (e.g., market data events).
  ```csharp
  Core.Globals.RandomDispatcher.BeginInvoke(new Action(() => {
      // UI updates here
  }));
  ```
- **Lifecycle Management:**
  - Initialize metadata in `OnStateChange` (`State.SetDefaults`).
  - Create menu items in `OnWindowCreated`.
  - Clean up resources and event handlers in `OnWindowDestroyed`.
- **Event Subscriptions:**
  - Subscribe to global events (e.g., `Account.AccountStatusUpdate`) in the constructor or `OnStateChange`.
  - Unsubscribe in `OnWindowDestroyed` or `Dispose` to prevent memory leaks.
- **Tab Naming:** Use the `TabName` property in `AddOnPage` to set the tab header (e.g., `TabName = "@INSTRUMENT_FULL";`).

## Common Pitfalls
- **Reference Paths:** Project references `NinjaTrader.Core.dll` and `NinjaTrader.Gui.dll` from the default installation path (`C:\Program Files (x86)\NinjaTrader 8\bin\`). Ensure these paths are correct for the environment.
- **XAML Integration:** NinjaTrader uses a specific resource dictionary. Use styles like `Style="{StaticResource MainMenuItem}"` for consistency.
- **State Management:** Check `State` property in `OnStateChange` before executing logic (e.g., `if (State == State.SetDefaults)`).

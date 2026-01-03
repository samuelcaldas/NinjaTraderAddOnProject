# NinjaTrader 8 AddOn Development Instructions

## Project Context
This is a C# AddOn for NinjaTrader 8. The project structure is non-standard, with all major classes defined in `AddOnFramework.cs` and UI defined in loose XAML (`AddOnFrameworkTab.xaml`) loaded as an embedded resource.

## Architecture & Key Components
- **Single-File Definition:** `AddOnFramework.cs` contains:
  - `AddOnFramework` (AddOnBase): Entry point, menu integration.
  - `AddOnFrameworkWindowFactory` (INTTabFactory): Creates windows/tabs.
  - `AddOnFrameworkWindow` (NTWindow): The main window container.
  - `AddOnFrameworkTab` (NTTabPage): The tab content and business logic.
- **UI Pattern (Loose XAML):**
  - XAML is defined in `AddOnFrameworkTab.xaml` (Build Action: **Embedded Resource**).
  - Loaded at runtime via `GetManifestResourceStream` and `XamlReader.Load`.
  - **NO Code-Behind:** Event handlers are attached manually in C# after loading.
  - Controls are retrieved using `LogicalTreeHelper.FindLogicalNode`.

## Critical Workflows
- **UI Loading:**
  ```csharp
  // Load XAML from Embedded Resource
  using (Stream stream = GetManifestResourceStream("AddOns.AddOnFrameworkTab.xaml")) {
      Page page = XamlReader.Load(stream) as Page;
      // Find controls manually
      outputBox = LogicalTreeHelper.FindLogicalNode(page.Content as DependencyObject, "outputBox") as TextBox;
  }
  ```
- **Instrument Linking:**
  - Implement `IInstrumentProvider`.
  - In the `Instrument` property setter, **ALWAYS** unsubscribe from previous instrument's events (MarketData, MarketDepth, etc.) before subscribing to the new one.
  ```csharp
  public Cbi.Instrument Instrument {
      set {
          if (instrument != null) {
              marketData.Update -= OnMarketData; // Unsubscribe old
          }
          instrument = value;
          if (instrument != null) {
              // Subscribe new
          }
      }
  }
  ```

## NinjaTrader Specific Patterns
- **Threading:**
  - Use `Dispatcher.InvokeAsync(() => { ... })` for WPF UI updates from background threads.
  - Use `Core.Globals.RandomDispatcher.BeginInvoke` for global NT actions.
- **Resource Cleanup:**
  - Unsubscribe from global events (`Account.AccountStatusUpdate`, `Connection.ConnectionStatusUpdate`) in `OnWindowDestroyed` or when switching accounts.
- **Tab Naming:** Set `TabName = "@INSTRUMENT_FULL";` in the constructor to enable dynamic tab headers.

## Common Pitfalls
- **XAML Events:** Do NOT define `Click="..."` or other event handlers in the XAML file. It will crash `XamlReader`. Attach them in C# code.
- **Namespace:** Ensure `GetManifestResourceStream` uses the correct resource name (e.g., `AddOns.AddOnFrameworkTab.xaml`).
- **References:** Ensure `NinjaTrader.Core` and `NinjaTrader.Gui` are referenced from the installation directory.

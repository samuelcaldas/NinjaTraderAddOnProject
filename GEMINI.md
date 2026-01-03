# NinjaTrader AddOn Project

## Project Overview

This project is a C# AddOn for **NinjaTrader 8**, designed to demonstrate the platform's framework capabilities. It provides a custom window ("AddOn Framework") with various tabs that allow users to interact with account data, market data, order management systems, and ATM strategies.

**Key Features:**
*   **Custom UI:** Creates a custom window accessible from the NinjaTrader "New" menu.
*   **Data Access:** Demonstrates how to request and display account values, executions, orders, positions, and strategies.
*   **Market Data:** Shows how to subscribe to real-time market data, market depth (Level 2), and fundamental data.
*   **Order Management:** Includes examples of submitting buy/sell market orders and framework-managed orders with stop-loss and profit targets.
*   **Event Handling:** Monitors connection status, simulation account resets, and news events.
*   **ATM Strategy Integration:** Demonstrates how to interact with and retrieve information from ATM (Advanced Trade Management) strategies.

## Architecture

The project follows a standard NinjaTrader AddOn structure:

*   **`AddOnFramework.cs`**:
    *   **`AddOnFramework` class**: Inherits from `AddOnBase`. It registers the AddOn and adds a menu item ("AddOn Framework") to the NinjaTrader Control Center's "New" menu.
    *   **`AddOnFrameworkWindowFactory` class**: Implements `INTTabFactory` to handle window and tab creation.
    *   **`AddOnFrameworkWindow` class**: Inherits from `NTWindow`, managing the `TabControl` and workspace persistence.
    *   **`AddOnFrameworkTab` class**: Inherits from `NTTabPage` and implements `IInstrumentProvider` and `IIntervalProvider`. This class contains the core logic for the AddOn's UI and interaction with NinjaTrader APIs.
*   **`AddOnFrameworkTab.xaml`** (implied): Defines the WPF user interface.
*   **`Info.xml`**: configuration file for NinjaTrader export.

## Development & Usage

### Prerequisites
*   **NinjaTrader 8** installed.
*   **Visual Studio** (or compatible C# IDE) for code editing and compilation.

### Installation
1.  **Clone/Download:** Get the source code to your local machine.
2.  **Open Solution:** Open the `.sln` file (if available) or create a new Class Library project in Visual Studio targeting **.NET Framework 4.8**.
3.  **References:** Ensure you reference the following NinjaTrader DLLs (usually found in `C:\Program Files (x86)\NinjaTrader 8\bin\`):
    *   `NinjaTrader.Core.dll`
    *   `NinjaTrader.Gui.dll`
    *   `NinjaTrader.Custom.dll` (optional, depending on usage)
    *   `NinjaTrader.Data.dll` (often part of Core/Gui context)
4.  **Build:** Compile the project to generate a `.dll` file.
5.  **Deploy:** Copy the generated `NinjaTraderAddOnProject.dll` to your NinjaTrader 8 Custom AddOns folder:
    *   `[Documents]\NinjaTrader 8\bin\Custom\AddOns`
6.  **Restart:** Restart NinjaTrader 8 for the AddOn to be loaded.

### Running the AddOn
1.  **Enable Custom Assemblies:** In NinjaTrader 8, go to **Tools > Options > General > Miscellaneous** and check **"Allow custom assembly loading"**.
2.  **Launch:** Go to **New > AddOn Framework**.
3.  **Interact:**
    *   Select an **Account** and **Instrument** using the selectors.
    *   Use the buttons in the "Account", "Data Access", "Orders", and "Misc." sections to trigger specific API calls and view the results in the output box on the right.

## Coding Conventions

*   **Namespaces:** The code uses `NinjaTrader.Gui.NinjaScript` to align with NinjaTrader conventions.
*   **UI Threading:** Interactions with the UI from data events (which run on background threads) are marshaled to the UI thread using `Dispatcher.InvokeAsync`.
*   **Resource Management:** The `Cleanup()` method (or equivalent logic in `AddOnFramework.cs`) is crucial to unsubscribe from events (e.g., `marketData.Update`, `Account.AccountStatusUpdate`) to prevent memory leaks.
*   **Persistence:** The `AddOnFrameworkWindow` implements `IWorkspacePersistence` to save and restore window state (selected account/instrument) when saving/loading NinjaTrader workspaces.

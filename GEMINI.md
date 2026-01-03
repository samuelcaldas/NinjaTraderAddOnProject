# NinjaTrader AddOn Project

## Project Overview

This project is a C# AddOn for NinjaTrader 8, designed to demonstrate the capabilities of the NinjaTrader framework. It provides a custom window with various tabs that allow users to interact with account data, market data, and order management systems.

**Key Features:**
*   **Custom UI:** Creates a custom window ("AddOn Framework") accessible from the NinjaTrader "New" menu.
*   **Data Access:** Demonstrates how to request and display account values, executions, orders, positions, and strategies.
*   **Market Data:** Shows how to subscribe to real-time market data, market depth (Level 2), and fundamental data.
*   **Order Management:** Includes examples of submitting buy/sell market orders and framework-managed orders with stop-loss and profit targets.
*   **Event Handling:** Monitors connection status, simulation account resets, and news events.

## Architecture

The project follows a standard NinjaTrader AddOn structure:

*   **`AddOnFramework.cs`**: The entry point. It inherits from `AddOnBase` to register the AddOn and add a menu item ("AddOn Framework") to the NinjaTrader Control Center's "New" menu. It also defines `AddOnFrameworkWindowFactory` to handle window creation.
*   **`AddOnFrameworkWindow`**: The main window class, inheriting from `NTWindow`. It manages the `TabControl` and workspace persistence.
*   **`AddOnPage.xaml`**: The WPF XAML definition for the AddOn's UI, utilizing NinjaTrader's custom controls (e.g., `AccountSelector`, `InstrumentSelector`).
*   **`AddOnPage.xaml.cs`**: The code-behind logic. It implements `IInstrumentProvider` and `IIntervalProvider`, handles button clicks, and manages subscriptions to NinjaTrader's data and event streams.

## Building and Installation

### Prerequisites
*   NinjaTrader 8 installed.
*   Visual Studio (or compatible C# IDE).

### Build Steps
1.  Open `NinjaTraderAddOnProject.sln` in Visual Studio.
2.  Build the solution (target framework is .NET Framework 4.8).

### Installation
1.  Locate the built assembly: `NinjaTraderAddOnProject.dll` (usually in `bin\Debug` or `bin\Release`).
2.  Copy the DLL to your NinjaTrader 8 custom AddOns folder:
    *   Path: `[Documents]\NinjaTrader 8\bin\Custom\AddOns`
3.  Restart NinjaTrader 8.

## Usage

1.  **Enable Custom Assemblies:** In NinjaTrader 8, go to **Tools > Options > General > Miscellaneous** and check **"Allow custom assembly loading"**.
2.  **Launch AddOn:** Go to **New > AddOn Framework**.
3.  **Interact:**
    *   Select an **Account** and **Instrument** using the selectors.
    *   Use the buttons in the "Account", "Data Access", "Orders", and "Misc." sections to trigger specific API calls and view the results in the output box on the right.

## Development Conventions

*   **Namespaces:** The primary namespace is `NinjaTraderAddOnProject`. The `AddOnFramework` class is in `NinjaTrader.Gui.NinjaScript` to align with NinjaTrader conventions.
*   **UI Threading:** Interactions with the UI from data events (which run on background threads) are marshaled to the UI thread using `Dispatcher.InvokeAsync`.
*   **Resource Management:** The `Cleanup()` method is overridden to unsubscribe from events (e.g., `marketData.Update`, `Account.AccountStatusUpdate`) to prevent memory leaks.
*   **Persistence:** The `AddOnFrameworkWindow` implements `IWorkspacePersistence` to save and restore window state (selected account/instrument) when saving/loading NinjaTrader workspaces.

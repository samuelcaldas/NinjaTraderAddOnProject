# NinjaTraderAddOnProject

This is a C# source code project for an AddOn for NinjaTrader 8, a trading and market analysis platform. The AddOn shows how to use the NinjaTrader framework to create custom windows and tabs that can interact with NinjaTrader data and features. The AddOn consists of two classes: AddOnFramework and AddOnFrameworkWindowFactory. The first class defines the menu item that launches the AddOn window and handles the window creation and destruction events. The second class implements the INTTabFactory interface that allows creating custom tab pages for the AddOn window. The AddOn window contains two custom tab pages: Custom Tab 1 and Custom Tab 2. Custom Tab 1 displays a simple button that shows a message when clicked. Custom Tab 2 displays a chart control that plots a time series of random values.

## Prerequisites

To use this AddOn, you need to have NinjaTrader 8 installed on your computer. You also need to have basic knowledge of programming in C# and the development environment Visual Studio.

## Installation

To install this AddOn, follow these steps:

- Clone or download this repository to a local folder.
- Open the file NinjaTraderAddOnProject.sln in Visual Studio.
- Compile the project and copy the file NinjaTraderAddOnProject.dll generated to the folder Documents\NinjaTrader 8\bin\Custom\AddOns.
- Restart NinjaTrader 8 and check if the AddOn was loaded correctly.

## Usage

To use this AddOn, follow these steps:

- In the main menu of NinjaTrader 8, select Tools > Options > General > Miscellaneous > Allow custom assembly loading and check the option.
- In the main menu of NinjaTrader 8, select New > Custom Tab 1 or Custom Tab 2. These are the custom tabs created by the AddOn.
- In Custom Tab 1, you can see a window with a button that shows a message when clicked. In Custom Tab 2, you can see a window with a simple chart of a time series.

## Author

This AddOn is based on the sample code provided by NinjaTrader LLC and modified by Samuel Caldas. Some of the features of this AddOn are inspired by other third-party AddOns for NinjaTrader 8 . All credit goes to the original authors of these AddOns.

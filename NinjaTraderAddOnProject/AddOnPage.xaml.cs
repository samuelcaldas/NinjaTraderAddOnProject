using NinjaTrader;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;

namespace NinjaTraderAddOnProject
{
	/// <summary>
	/// Interaction logic for Page1.xaml
	/// </summary>
	public partial class AddOnPage : NinjaTrader.Gui.Tools.IInstrumentProvider, NinjaTrader.Gui.Tools.IIntervalProvider
	{
		#region Variables
		// ADDONFRAMEWORK SECTION
		private AddOnFrameworkDisplay outputType;
		public enum AddOnFrameworkDisplay          // enum to help determine what to display in our output box
		{
			acctExec,
			acctOrders,
			acctPos,
			acctStrat,
			acctValues,
			buyMarket,
			connectionInfo,
			connectKinetickEOD,
			frameworkManaged,
			fundamentalData,
			marketData,
			marketDataSnapshot,
			marketDepthAsk,
			marketDepthBid,
			onAccountItemUpdate,
			onAccountStatusUpdate,
			onConnectionStatusUpdate,
			onExecutionUpdate,
			onNews,
			onOrderUpdate,
			onPositionUpdate,
			onSimulationAccountReset,
			requestData,
			realtimeData,
			sellMarket,
		}

		// private int                          barCount = 0;               // Used if processing real-time bars only on bar closes
		private NinjaTrader.Cbi.Instrument instrument;
		private bool barsRequestSubscribed = false;
		private NinjaTrader.Data.BarsRequest barsRequest;
		private NinjaTrader.Data.MarketData marketData;
		private NinjaTrader.Data.MarketDepth<NinjaTrader.Data.MarketDepthRow> marketDepth;
		private NinjaTrader.Data.FundamentalData fundamentalData;
		private NinjaTrader.Data.NewsSubscription newsSubscription;
		private NinjaTrader.Data.NewsItems newsItems;

		private Connection connection;


		private Order stopLoss;
		private Order profitTarget;
		private Order frameworkEntryOrder;
		private Order entryOrder;

		#endregion
		public AddOnPage()
		{
			InitializeComponent();
			// Sets the tab header name to be the currently selected instrument's name
			TabName = "@INSTRUMENT_FULL";

			// Subscribe to account status updates. This event will fire as connection.Status of the hosting connection changes
			Account.AccountStatusUpdate += OnAccountStatusUpdate;

			// Subscribe to sim account resets (Also happens when rewinding/fast forwarding Playback connection)
			Account.SimulationAccountReset += OnSimulationAccountReset;

			// Subscribe to connection status events
			Connection.ConnectionStatusUpdate += OnConnectionStatusUpdate;

			// Subscribe to news
			newsSubscription = new NinjaTrader.Data.NewsSubscription();
			newsSubscription.Update += OnNews;
			newsItems = new NinjaTrader.Data.NewsItems(10);     // Maintain the last 10 news items

			accountSelector.SelectionChanged += (o, args) =>
			{
				if (accountSelector.SelectedAccount != null)
				{
					// Unsubscribe to any prior account subscriptions
					accountSelector.SelectedAccount.AccountItemUpdate -= OnAccountItemUpdate;
					accountSelector.SelectedAccount.ExecutionUpdate -= OnExecutionUpdate;
					accountSelector.SelectedAccount.OrderUpdate -= OnOrderUpdate;
					accountSelector.SelectedAccount.PositionUpdate -= OnPositionUpdate;

					// Subscribe to new account subscriptions
					accountSelector.SelectedAccount.AccountItemUpdate += OnAccountItemUpdate;
					accountSelector.SelectedAccount.ExecutionUpdate += OnExecutionUpdate;
					accountSelector.SelectedAccount.OrderUpdate += OnOrderUpdate;
					accountSelector.SelectedAccount.PositionUpdate += OnPositionUpdate;
				}
			};
		}

		private void OnButtonClick(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;

			#region Behavior for Account Section Buttons
			// When user presses the Account Values button
			if (button != null && ReferenceEquals(button, acctValuesButton))
			{
				outputType = AddOnFrameworkDisplay.acctValues;
				outputBox.Text = "ACCOUNT VALUES";

				if (accountSelector == null || accountSelector.SelectedAccount == null)
				{
					outputBox.AppendText(Environment.NewLine + "No account selected.");
					return;
				}

				outputBox.AppendText(string.Format("{0}Name: {1}{0}Connection: {2}{0}BuyingPower: {3}{0}CashValue: {4}{0}InitialMargin: {5}{0}MaintenanceMargin: {6}{0}NetLiquidation: {7}{0}NetLiquidationByCurrency: {8}{0}RealizedProfitLoss: {9}{0}TotalCashBalance: {10}{0}",
					Environment.NewLine,
					accountSelector.SelectedAccount.Name,
					accountSelector.SelectedAccount.Connection.Options.Name,
					accountSelector.SelectedAccount.Get(AccountItem.BuyingPower, Currency.UsDollar),
				accountSelector.SelectedAccount.Get(AccountItem.CashValue, Currency.UsDollar),
				accountSelector.SelectedAccount.Get(AccountItem.InitialMargin, Currency.UsDollar),
				accountSelector.SelectedAccount.Get(AccountItem.MaintenanceMargin, Currency.UsDollar),
				accountSelector.SelectedAccount.Get(AccountItem.NetLiquidation, Currency.UsDollar),
				accountSelector.SelectedAccount.Get(AccountItem.NetLiquidationByCurrency, Currency.UsDollar),
				accountSelector.SelectedAccount.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar),
				accountSelector.SelectedAccount.Get(AccountItem.TotalCashBalance, Currency.UsDollar)));

			}

			// When user presses the Account Executions button
			if (button != null && ReferenceEquals(button, acctExecButton))
			{
				outputType = AddOnFrameworkDisplay.acctExec;
				outputBox.Text = "ACCOUNT EXECUTIONS";

				if (accountSelector == null || accountSelector.SelectedAccount == null)
				{
					outputBox.AppendText(Environment.NewLine + "No account selected.");
					return;
				}

				// Executions since last connect + NT lookback period for historical executions
				lock (accountSelector.SelectedAccount.Executions)
					foreach (Execution execution in accountSelector.SelectedAccount.Executions)
						outputBox.AppendText(string.Format("{0}Instrument: {1}{0}Quantity: {2}{0}Price: {3}{0}Time: {4}{0}ExecutionID: {5}{0}Exchange: {6}{0}MarketPosition: {7}{0}OrderID: {8}{0}Name: {9}{0}Commission: {10}{0}Rate: {11}{0}",
							Environment.NewLine,
							execution.Instrument.FullName,
							execution.Quantity,
							execution.Price,
							execution.Time,
							execution.ExecutionId,
							execution.Exchange,
							execution.MarketPosition,
							execution.OrderId,
							execution.Name,
							execution.Commission,
							execution.Rate));
			}

			// When user presses the Account Orders button
			if (button != null && ReferenceEquals(button, acctOrdersButton))
			{
				outputType = AddOnFrameworkDisplay.acctOrders;
				outputBox.Text = "ACCOUNT ORDERS";

				if (accountSelector == null || accountSelector.SelectedAccount == null)
				{
					outputBox.AppendText(Environment.NewLine + "No account selected.");
					return;
				}

				// Iterate through all orders (open and closed)
				lock (accountSelector.SelectedAccount.Orders)
					foreach (Order order in accountSelector.SelectedAccount.Orders)
						outputBox.AppendText(string.Format("{0}Instrument: {1}{0}OrderAction: {2}{0}OrderType: {3}{0}Quantity: {4}{0}LimitPrice: {5}{0}StopPrice: {6}{0}OrderState: {7}{0}Filled: {8}{0}AverageFillPrice: {9}{0}Name: {10}{0}OCO: {11}{0}TimeInForce: {12}{0}OrderID: {13}{0}Time: {14}{0}",
							Environment.NewLine,
							order.Instrument.FullName,
							order.OrderAction,
							order.OrderType,
							order.Quantity,
							order.LimitPrice,
							order.StopPrice,
							order.OrderState,
							order.Filled,
							order.AverageFillPrice,
							order.Name,
							order.Oco,
							order.TimeInForce,
							order.OrderId,
							order.Time));
			}

			// When user presses the Account Positions button
			if (button != null && ReferenceEquals(button, acctPosButton))
			{
				outputType = AddOnFrameworkDisplay.acctPos;
				outputBox.Text = "ACCOUNT POSITIONS";

				if (accountSelector == null || accountSelector.SelectedAccount == null)
				{
					outputBox.AppendText(Environment.NewLine + "No account selected.");
					return;
				}

				// Iterate through all open positions
				lock (accountSelector.SelectedAccount.Positions)
					foreach (Position position in accountSelector.SelectedAccount.Positions)
						outputBox.AppendText(string.Format("{0}Instrument: {1}{0}MarketPosition: {2}{0}Quantity: {3}{0}AveragePrice: {4}{0}UnrealizedPnL: {5}{0}Connection: {6}{0}",
							Environment.NewLine,
							position.Instrument.FullName,
							position.MarketPosition,
							position.Quantity,
							position.AveragePrice,
							position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, double.MinValue),
							//(position.Instrument.MarketData.Last != null ? position.GetProfitLoss(position.Instrument.MarketData.Last.Price, PerformanceUnit.Currency).ToString() : "(unknown)"),
							accountSelector.SelectedAccount.Connection.Options.Name));
			}

			// When user presses the Account Strategies button
			if (button != null && ReferenceEquals(button, acctStratButton))
			{
				outputType = AddOnFrameworkDisplay.acctStrat;
				outputBox.Text = "ACCOUNT STRATEGIES";

				if (accountSelector == null || accountSelector.SelectedAccount == null)
				{
					outputBox.AppendText(Environment.NewLine + "No account selected.");
					return;
				}

				// Iterate through all ATM and NS strategies (active, recovered upon last connect, or deactived since last connect)
				lock (accountSelector.SelectedAccount.Strategies)
					foreach (StrategyBase strategy in accountSelector.SelectedAccount.Strategies)
						outputBox.AppendText(string.Format("{0}Name: {1}{0}ATM Template Name: {2}{0}Instrument: {3}{0}State: {4}{0}Category: {5}{0}",
							Environment.NewLine,
							strategy.Name,
							strategy.Template,
							strategy.Instruments[0].FullName,       // This is the primary instrument. There might be secondary instruments on NS strategies
							strategy.State,
							strategy.Category));
			}

			// When user presses the OnAccountItemUpdate button
			if (button != null && ReferenceEquals(button, onAccountItemUpdateButton))
			{
				outputType = AddOnFrameworkDisplay.onAccountItemUpdate;
				outputBox.Text = "OnAccountItemUpdate() Monitor";
				if (accountSelector.SelectedAccount == null)
					outputBox.AppendText(Environment.NewLine + "No account selected.");
			}

			// When user presses the OnAccountStatusUpdate button
			if (button != null && ReferenceEquals(button, onAccountStatusUpdateButton))
			{
				outputType = AddOnFrameworkDisplay.onAccountStatusUpdate;
				outputBox.Text = "OnAccountStatusUpdate() Monitor";
			}
			#endregion

			#region Behavior for Data Access Section Buttons
			// When user presses the Request Data button
			if (button != null && ReferenceEquals(button, requestDataButton))
			{
				outputType = AddOnFrameworkDisplay.requestData;
				outputBox.Text = "LOADING DATA...";

				// Unsubscribe to any old bars requests
				if (barsRequest != null)
				{
					if (barsRequestSubscribed)
					{
						barsRequest.Update -= OnBarUpdate;
						barsRequestSubscribed = false;
					}
					barsRequest = null;
				}

				barsRequest = DoBarsRequest(Instrument, Convert.ToInt32(daysBackSelector.Text.ToString()), false);
			}

			// When user presses the Realtime Data button
			if (button != null && ReferenceEquals(button, realtimeDataButton))
			{
				outputType = AddOnFrameworkDisplay.realtimeData;
				outputBox.Text = "LOADING DATA...";

				// Unsubscribe to any old bars requests
				if (barsRequest != null)
				{
					if (barsRequestSubscribed)
					{
						barsRequest.Update -= OnBarUpdate;
						barsRequestSubscribed = false;
					}
					barsRequest = null;
				}

				barsRequest = DoBarsRequest(Instrument, Convert.ToInt32(daysBackSelector.Text.ToString()), true);
			}

			// When user presses the Fundamental Data button
			if (button != null && ReferenceEquals(button, fundamentalDataButton))
			{
				outputType = AddOnFrameworkDisplay.fundamentalData;
				if (fundamentalData != null)
				{
					outputBox.Text = string.Format("FUNDAMENTAL SNAPSHOT{0}Average Daily Volume: {1}{0}Beta: {2}{0}Calendar Year High: {3}{0}Calendar Year High Date: {4}{0}Calendar Year Low: {5}{0}Calendar Year Low Date: {6}{0}Current Ratio: {7}{0}Dividend Amount: {8}{0}Dividend Pay Date: {9}{0}Dividend Yield: {10}{0}Earnings Per Share: {11}{0}5 Yrs Growth Percentage: {12}{0}High 52 Weeks: {13}{0}High 52 Weeks Date: {14}{0}Historical Volatility: {15}{0}Insider Owned: {16}{0}Low 52 Weeks: {17}{0}Low 52 Weeks Date: {18}{0}Market Cap: {19}{0}Next Year EPS: {20}{0}Percent Held By Institutions: {21}{0}Price Earnings Ratio: {22}{0}Revenue Per Share: {23}{0}Shares Outstanding: {24}{0}Short Interest: {25}{0}Short Interest Ratio: {26}{0}VWAP: {27}",
						Environment.NewLine,
						fundamentalData.AverageDailyVolume,
						fundamentalData.Beta,
						fundamentalData.CalendarYearHigh,
						fundamentalData.CalendarYearHighDate,
						fundamentalData.CalendarYearLow,
						fundamentalData.CalendarYearLowDate,
						fundamentalData.CurrentRatio,
						fundamentalData.DividendAmount,
						fundamentalData.DividendPayDate,
						fundamentalData.DividendYield,
						fundamentalData.EarningsPerShare,
						fundamentalData.FiveYearsGrowthPercentage,
						fundamentalData.High52Weeks,
						fundamentalData.High52WeeksDate,
						fundamentalData.HistoricalVolatility,
						fundamentalData.InsiderOwned,
						fundamentalData.Low52Weeks,
						fundamentalData.Low52WeeksDate,
						fundamentalData.MarketCap,
						fundamentalData.NextYearsEarningsPerShare,
						fundamentalData.PercentHeldByInstitutions,
						fundamentalData.PriceEarningsRatio,
						fundamentalData.RevenuePerShare,
						fundamentalData.SharesOutstanding,
						fundamentalData.ShortInterest,
						fundamentalData.ShortInterestRatio,
						fundamentalData.VWAP);
				}
				else
					outputBox.Text = string.Format("FUNDAMENTAL SNAPSHOT{0}No instrument selected.", Environment.NewLine);
			}

			// When user presses the Market Data Subscription button
			if (button != null && ReferenceEquals(button, marketDataButton))
			{
				outputType = AddOnFrameworkDisplay.marketData;
				if (marketData == null)
					outputBox.Text = string.Format("MARKET DATA{0}No instrument selected.", Environment.NewLine);
				else
					outputBox.Text = "MARKET DATA";
			}

			// When user presses the Market Data Snapshot button
			if (button != null && ReferenceEquals(button, marketDataSnapshotButton))
			{
				outputType = AddOnFrameworkDisplay.marketDataSnapshot;
				if (marketData != null)
				{
					outputBox.Text = string.Format("MARKET SNAPSHOT{0}Last: {1}{0}Ask: {2}{0}Bid: {3}{0}DailyHigh: {4}{0}DailyLow: {5}{0}LastClose: {6}{0}Opening: {7}{0}OpenInterest: {8}{0}Settlement: {9}",
						Environment.NewLine,
						(marketData.Last != null ? marketData.Last.Price.ToString() : "(null)"),
						(marketData.Ask != null ? marketData.Ask.Price.ToString() : "(null)"),
						(marketData.Bid != null ? marketData.Bid.Price.ToString() : "(null)"),
						(marketData.DailyHigh != null ? marketData.DailyHigh.Price.ToString() : "(null)"),
						(marketData.DailyLow != null ? marketData.DailyLow.Price.ToString() : "(null)"),
						(marketData.LastClose != null ? marketData.LastClose.Price.ToString() : "(null)"),
						(marketData.Opening != null ? marketData.Opening.Price.ToString() : "(null)"),
						(marketData.OpenInterest != null ? marketData.OpenInterest.Price.ToString() : "(null)"),
						(marketData.Settlement != null ? marketData.Settlement.Price.ToString() : "(null)"));
				}
				else
					outputBox.Text = string.Format("MARKET SNAPSHOT{0}No instrument selected.", Environment.NewLine);
			}

			// When user presses the Market Depth Ask button
			if (button != null && ReferenceEquals(button, marketDepthAskButton))
			{
				outputType = AddOnFrameworkDisplay.marketDepthAsk;
				if (marketDepth != null)
				{
					outputBox.Text = "ASK LADDER";
					// Iterate through the ASK side of the price ladder
					for (int i = 0; i < marketDepth.Asks.Count; i++)
						outputBox.AppendText(string.Format("{0}Position: {2}{1}Price: {3}{1}Volume: {4}",
							Environment.NewLine,
							"\t",
							i,
							marketDepth.Asks[i].Price,
							marketDepth.Asks[i].Volume));
				}
				else
					outputBox.Text = string.Format("ASK LADDER{0}No instrument selected.", Environment.NewLine);
			}

			// When user presses the Market Depth Bid button
			if (button != null && ReferenceEquals(button, marketDepthBidButton))
			{
				outputType = AddOnFrameworkDisplay.marketDepthBid;
				if (marketDepth != null)
				{
					outputBox.Text = "BID LADDER";
					// Iterate through the BID side of the price ladder
					for (int i = 0; i < marketDepth.Bids.Count; i++)
						outputBox.AppendText(string.Format("{0}Position: {2}{1}Price: {3}{1}Volume: {4}",
							Environment.NewLine,
							"\t",
							i,
							marketDepth.Bids[i].Price,
							marketDepth.Bids[i].Volume));
				}
				else
					outputBox.Text = string.Format("BID LADDER{0}No instrument selected.", Environment.NewLine);
			}
			#endregion

			#region Behavior for Orders Section Buttons
			// When user presses the Buy Market button
			if (button != null && ReferenceEquals(button, buyMarketButton))
			{
				outputType = AddOnFrameworkDisplay.buyMarket;
				outputBox.Text = "SUBMIT BUY MARKET ORDER";

				if (accountSelector == null || accountSelector.SelectedAccount == null)
				{
					outputBox.AppendText(Environment.NewLine + "No account selected.");
					return;
				}

				if (Instrument == null)
				{
					outputBox.AppendText(Environment.NewLine + "No instrument selected.");
					return;
				}

				// Submit our Buy Market order with the selected ATM strategy
				if (atmStrategySelector.SelectedAtmStrategy != null)
				{
					/* When submitted orders with ATM strategies you MUST set the 'name' property to "Entry" in order for it to work.
                    Otherwise the entry order will be stuck in OrderState.Initialized and no ATM strategy will be started */
					entryOrder = accountSelector.SelectedAccount.CreateOrder(Instrument, OrderAction.Buy, OrderType.Market, tifSelector.SelectedTif, qudSelector.Value, 0, 0, string.Empty, "Entry", null);
					NinjaTrader.NinjaScript.AtmStrategy.StartAtmStrategy(atmStrategySelector.SelectedAtmStrategy, entryOrder);
				}
				// If no ATM strategy selected, submit our Buy Market order alone
				else
				{
					entryOrder = accountSelector.SelectedAccount.CreateOrder(Instrument, OrderAction.Buy, OrderType.Market, tifSelector.SelectedTif, qudSelector.Value, 0, 0, string.Empty, "AddOnFramework - Buy Market", null);
					accountSelector.SelectedAccount.Submit(new[] { entryOrder });
				}
			}

			// When user presses the Sell Market button
			if (button != null && ReferenceEquals(button, sellMarketButton))
			{
				outputType = AddOnFrameworkDisplay.sellMarket;
				outputBox.Text = "SUBMIT SELL MARKET ORDER";

				if (accountSelector == null || accountSelector.SelectedAccount == null)
				{
					outputBox.AppendText(Environment.NewLine + "No account selected.");
					return;
				}

				if (Instrument == null)
				{
					outputBox.AppendText(Environment.NewLine + "No instrument selected.");
					return;
				}

				// Submit our Sell Market order with the selected ATM strategy
				if (atmStrategySelector.SelectedAtmStrategy != null)
				{
					/* When submitted orders with ATM strategies you MUST set the 'name' property to "Entry" in order for it to work.
                    Otherwise the entry order will be stuck in OrderState.Initialized and no ATM strategy will be started */
					entryOrder = accountSelector.SelectedAccount.CreateOrder(Instrument, OrderAction.Sell, OrderType.Market, tifSelector.SelectedTif, qudSelector.Value, 0, 0, string.Empty, "Entry", null);
					NinjaTrader.NinjaScript.AtmStrategy.StartAtmStrategy(atmStrategySelector.SelectedAtmStrategy, entryOrder);
				}
				// If no ATM strategy selected, submit our Sell Market order alone
				else
				{
					entryOrder = accountSelector.SelectedAccount.CreateOrder(Instrument, OrderAction.Sell, OrderType.Market, tifSelector.SelectedTif, qudSelector.Value, 0, 0, string.Empty, "AddOnFramework - Sell Market", null);
					accountSelector.SelectedAccount.Submit(new[] { entryOrder });
				}
			}

			// When user presses the Framework Managed button
			if (button != null && ReferenceEquals(button, frameworkManagedButton))
			{
				outputType = AddOnFrameworkDisplay.frameworkManaged;
				outputBox.Text = "FRAMEWORK MANAGED BUY MARKET ORDER";

				if (accountSelector == null || accountSelector.SelectedAccount == null)
				{
					outputBox.AppendText(Environment.NewLine + "No account selected.");
					return;
				}

				if (Instrument == null)
				{
					outputBox.AppendText(Environment.NewLine + "No instrument selected.");
					return;
				}

				// Request real-time data for the management of our stops/targets after we enter into the market
				if (barsRequest != null)
				{
					if (barsRequestSubscribed)
					{
						barsRequest.Update -= OnBarUpdate;
						barsRequestSubscribed = false;
					}
					barsRequest = null;
				}
				barsRequest = DoBarsRequest(Instrument, Convert.ToInt32(daysBackSelector.Text.ToString()), true);

				/* Submit our entry order to be protected by the framework
            	See OnOrderUpdate() for the stop/targets.
				See OnBarUpdate() for how to modify/cancel orders */
				frameworkEntryOrder = accountSelector.SelectedAccount.CreateOrder(Instrument, OrderAction.Buy, OrderType.Market, tifSelector.SelectedTif, qudSelector.Value, 0, 0, string.Empty, "AddOnFramework - Managed Buy Market", null);
				accountSelector.SelectedAccount.Submit(new[] { frameworkEntryOrder });
			}

			// When user presses the OnOrderUpdate button
			if (button != null && ReferenceEquals(button, onOrderUpdateButton))
			{
				outputType = AddOnFrameworkDisplay.onOrderUpdate;
				outputBox.Text = "OnOrderUpdate() Monitor";
			}

			// When user presses the OnExecutionUpdate button
			if (button != null && ReferenceEquals(button, onExecutionUpdateButton))
			{
				outputType = AddOnFrameworkDisplay.onExecutionUpdate;
				outputBox.Text = "OnExecutionUpdate() Monitor";
			}

			// When user presses the OnPositionUpdate button
			if (button != null && ReferenceEquals(button, onPositionUpdateButton))
			{
				outputType = AddOnFrameworkDisplay.onPositionUpdate;
				outputBox.Text = "OnPositionUpdate() Monitor";
			}
			#endregion

			#region Behavior for Misc Section Buttons
			// When user presses the Connect Kinetick EOD button
			if (button != null && ReferenceEquals(button, connectKinetickEODButton))
			{
				if (connection == null)
				{
					// Establish connection
					connection = Connect("Kinetick – End Of Day (Free)");
				}
				else
					connection.Disconnect();
			}

			// When user presses the Connection Info button
			if (button != null && ReferenceEquals(button, connectionInfoButton))
			{
				outputType = AddOnFrameworkDisplay.connectionInfo;
				outputBox.Text = "CONNECTION INFORMATION";

				// Access information about all connected connections
				lock (Connection.Connections)
					Connection.Connections.ToList().ForEach(c => outputBox.AppendText(string.Format("{0}Connection: {1}{0}Provider: {2}{0}Mode: {3}{0}",
						Environment.NewLine,
						c.Options.Name,
						c.Options.Provider,
						c.Options.Mode
					)));
			}

			// When user presses the OnConnectionStatusUpdate button
			if (button != null && ReferenceEquals(button, onConnectionStatusUpdateButton))
			{
				outputType = AddOnFrameworkDisplay.onConnectionStatusUpdate;
				outputBox.Text = "OnConnectionStatusUpdate() Monitor";
			}

			// When user presses the OnSimulationAccountReset button
			if (button != null && ReferenceEquals(button, onSimulationAccountResetButton))
			{
				outputType = AddOnFrameworkDisplay.onSimulationAccountReset;
				outputBox.Text = "OnSimulationAccountReset() Monitor";
			}

			// When user presses the OnNews button
			if (button != null && ReferenceEquals(button, onNewsButton))
			{
				outputType = AddOnFrameworkDisplay.onNews;
				outputBox.Text = "OnNews() Monitor";
			}
			#endregion
		}

		AccountItemEventArgs myArgs;

		#region Methods for Account Section
		// This method is fired on any change of an 'Account Value'
		private void OnAccountItemUpdate(object sender, AccountItemEventArgs e)
		{
			try
			{
				myArgs = e;
				// Only display messages from this method if our output box is displaying this category of messages
				if (outputType != AddOnFrameworkDisplay.onAccountItemUpdate)
					return;

				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				Dispatcher.InvokeAsync(() =>
				{
					outputBox.AppendText(string.Format("{0}Account: {1}{0}AccountItem: {2}{0}Value: {3}",
						Environment.NewLine,
						e.Account.Name,
						e.AccountItem,
						e.Value));
				});
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnAccountItemUpdate Exception: " + error.ToString();
				});
			}
		}

		// This method is fired on any status change of any account
		private void OnAccountStatusUpdate(object sender, AccountStatusEventArgs e)
		{
			try
			{
				// Only display messages from this method if our output box is displaying this category of messages
				if (outputType != AddOnFrameworkDisplay.onAccountStatusUpdate)
					return;

				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				Dispatcher.InvokeAsync(() =>
				{
					outputBox.AppendText(string.Format("{0}Account: {1}{0}Status: {2}",
						Environment.NewLine,
						e.Account.Name,
						e.Status));
				});
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnAccountStatusUpdate Exception: " + error.ToString();
				});
			}
		}
		#endregion

		#region Methods for Data Access Section
		// This method is fired when our instrument selector changes instruments
		private void OnInstrumentChanged(object sender, EventArgs e)
		{
			Instrument = sender as NinjaTrader.Cbi.Instrument;
		}

		// This method is fired when our interval selector changes intervals
		private void OnIntervalChanged(object sender, BarsPeriodEventArgs args)
		{
			if (args.BarsPeriod == null)
				return;

			BarsPeriod = args.BarsPeriod;
			//PropagateIntervalChange(args.BarsPeriod); THIS IS NOT AVAILABLE IN BETA 10, BUT WILL BE IN BETA 11. NEW VERSION WILL BE UPLOADED AT THAT TIME.
		}

		// This method is fired no fundamental data events. Note: snapshot data provided right on subscription
		private void OnFundamentalData(object sender, NinjaTrader.Data.FundamentalDataEventArgs e)
		{
			try
			{
				// Only display messages from this method if our output box is displaying this category of messages
				if (outputType != AddOnFrameworkDisplay.fundamentalData)
					return;

				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				Dispatcher.InvokeAsync(() =>
				{
					// Depending on the type of fundamental data, the value would be stored accordingly
					switch (e.FundamentalDataType)
					{

						#region FundamentalDataType
						case FundamentalDataType.AverageDailyVolume:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.Beta:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.CalendarYearHigh:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.CalendarYearHighDate:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DateTimeValue);
						break;
						case FundamentalDataType.CalendarYearLow:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.CalendarYearLowDate:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DateTimeValue);
						break;
						case FundamentalDataType.CurrentRatio:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.DividendAmount:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.DividendPayDate:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.DividendYield:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.EarningsPerShare:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.FiveYearsGrowthPercentage:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.High52Weeks:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.High52WeeksDate:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DateTimeValue);
						break;
						case FundamentalDataType.HistoricalVolatility:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.Low52Weeks:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.Low52WeeksDate:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DateTimeValue);
						break;
						case FundamentalDataType.MarketCap:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.NextYearsEarningsPerShare:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.PercentHeldByInstitutions:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.PriceEarningsRatio:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.RevenuePerShare:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.SharesOutstanding:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.LongValue);
						break;
						case FundamentalDataType.ShortInterest:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.ShortInterestRatio:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						case FundamentalDataType.VWAP:
						outputBox.Text = string.Format("Type: {0} Value: {1}", e.FundamentalDataType, e.DoubleValue);
						break;
						#endregion

					}
				});
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnFundamentalData Exception: " + error.ToString();
				});
			}
		}

		// This method is fired after the market data snapshot data is updated
		private void OnMarketData(object sender, NinjaTrader.Data.MarketDataEventArgs e)
		{
			try
			{
				// Only display messages from this method if our output box is displaying this category of messages
				if (outputType != AddOnFrameworkDisplay.marketData)
					return;

				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				Dispatcher.InvokeAsync(() =>
				{
					// Process Last type market data events
					if (e.MarketDataType == MarketDataType.Last)
						outputBox.Text = string.Format("MARKET DATA{0}Instrument: {1}{0}Time: {2}{0}{3} Price: {4}{0}Ask Price: {5}{0}Bid Price: {6}{0}Volume: {7}{0}",
							Environment.NewLine,
							e.Instrument.FullName,
							e.Time,
							e.MarketDataType,
							e.Price,
							e.Ask,
							e.Bid,
							e.Volume);

					if (e.IsReset == true)
						outputBox.Text = string.Format("MARKET DATA - Disconnected{0}IsReset: {1}",
							Environment.NewLine,
							e.IsReset);             // Indicates that the UI should be reset. e.g. on disconnect
				});
			}
			catch (Exception error)
			{
				if (outputType != AddOnFrameworkDisplay.marketData)
					return;

				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnMarketData Exception: " + error.ToString();
				});
			}
		}

		// This method is fired after the market depth snapshot data is updated
		private void OnMarketDepth(object sender, NinjaTrader.Data.MarketDepthEventArgs e)
		{
			try
			{
				if (outputType == AddOnFrameworkDisplay.marketDepthAsk)
				{
					/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                    influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
					Dispatcher.InvokeAsync(() =>
					{
						if (e.IsReset == true)
						{
							outputBox.Text = string.Format("ASK LADDER - Disconnected{0}IsReset: {1}",
								Environment.NewLine,
								e.IsReset);             // Indicates that the UI should be reset. e.g. on disconnect
							return;
						}

						outputBox.Text = "ASK LADDER";
						// iterate through ASK side of the price ladded
						for (int i = 0; i < marketDepth.Asks.Count; i++)
							outputBox.AppendText(string.Format("{0}Position: {2}{1}Price: {3}{1}Volume: {4}",
								Environment.NewLine,
								"\t",
								i,
								marketDepth.Asks[i].Price,
								marketDepth.Asks[i].Volume));
					});
				}
				else if (outputType == AddOnFrameworkDisplay.marketDepthBid)
				{
					/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                    influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
					Dispatcher.InvokeAsync(() =>
					{
						if (e.IsReset == true)
						{
							outputBox.Text = string.Format("BID LADDER - Disconnected{0}IsReset: {1}",
								Environment.NewLine,
								e.IsReset);             // Indicates that the UI should be reset. e.g. on disconnect
							return;
						}

						outputBox.Text = "BID LADDER";
						// iterate through ASK side of the price ladded
						for (int i = 0; i < marketDepth.Bids.Count; i++)
							outputBox.AppendText(string.Format("{0}Position: {2}{1}Price: {3}{1}Volume: {4}",
								Environment.NewLine,
								"\t",
								i,
								marketDepth.Bids[i].Price,
								marketDepth.Bids[i].Volume));
					});
				}
				else
					return;
			}
			catch (Exception error)
			{
				if (outputType == AddOnFrameworkDisplay.marketDepthAsk || outputType == AddOnFrameworkDisplay.marketDepthBid)
				{
					Dispatcher.InvokeAsync(() =>
					{
						// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
						outputBox.Text = "AddOnFramework - OnMarketDepth Exception: " + error.ToString();
					});
				}
				else
					return;
			}
		}

		// This method is fired on real-time bar events
		private void OnBarUpdate(object sender, NinjaTrader.Data.BarsUpdateEventArgs e)
		{
			try
			{
				/* Modify profit target order on latest price update if limit price is more than 14 ticks away from current price
                Note: If a single tick updates several bars, this modification only occurs once on the latest bar */
				if (profitTarget != null)
				{
					// If profit target is now 14 ticks away from the close price, modify the profit target down
					if (profitTarget.LimitPrice > e.BarsSeries.GetClose(e.MaxIndex) + 14 * Instrument.MasterInstrument.TickSize)
					{
						/* For modifying orders use the follow:
                        .LimitPriceChanged for limit price changes
                        .StopPriceChanged for stop price changes
                        .QuantityChanged for quantity changes */
						profitTarget.LimitPriceChanged = e.BarsSeries.GetClose(e.MaxIndex) + 4 * Instrument.MasterInstrument.TickSize;
						profitTarget.Account.Change(new[] { profitTarget });

						// If you wanted to just cancel the order you could use account.Cancel()
						// profitTarget.Account.Cancel(new[] { profitTarget });
					}
				}

				// Only display messages from this method if our output box is displaying this category of messages
				if (outputType != AddOnFrameworkDisplay.realtimeData)
					return;

				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				Dispatcher.InvokeAsync(() =>
				{
					/* Depending on the BarsPeriod type of your barsRequest you can have situations where more than one bar is updated by a single tick
                    Be sure to process the full range of updated bars to ensure you did not miss a bar. */

					// Option 1. If you want to process updated bars on each tick
					for (int i = e.MinIndex; i <= e.MaxIndex; i++)
					{
						// Processing every single tick
						outputBox.Text = string.Format("REALTIME BARS{0}Time: {1}{0}Open: {2}{0}High: {3}{0}Low: {4}{0}Close: {5}",
							Environment.NewLine,
							e.BarsSeries.GetTime(i),
							e.BarsSeries.GetOpen(i),
							e.BarsSeries.GetHigh(i),
							e.BarsSeries.GetLow(i),
							e.BarsSeries.GetClose(i));
					}

					#region Option 2. If you want to process bars at the close of each bar
					/* if (barCount == -1)
                        barCount = e.MinIndex;

                    if (e.MaxIndex == barCount)
                        return;
                    else if (e.MaxIndex > barCount)
                    {
                        for (int i = e.MinIndex; i <= e.MaxIndex; i++)
                        {
                            // Processing the newly finished bars.
                            // Note: We only know a bar closed when a new bar forms. This means we need to decrement our index by 1
                            // otherwise we would be accessing the newly formed bar instead of the finished bar.
                            outputBox.Text = string.Format("REALTIME BARS (On bar close){0}Time: {1}{0}Open: {2}{0}High: {3}{0}Low: {4}{0}Close: {5}",
                                Environment.NewLine,
                                e.BarsSeries.GetTime(i - 1),
                                e.BarsSeries.GetOpen(i - 1),
                                e.BarsSeries.GetHigh(i - 1),
                                e.BarsSeries.GetLow(i - 1),
                                e.BarsSeries.GetClose(i - 1));
                        }

                        barCount = e.MaxIndex;
                    } */
					#endregion
				});
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnBarUpdate Exception: " + error.ToString();
				});
			}
		}

		// Get bars. This is also where we subscribe to real-time bar events.
		private NinjaTrader.Data.BarsRequest DoBarsRequest(Instrument instrument, int lookBackPeriod, bool subscribeRealTimeData)
		{
			try
			{
				NinjaTrader.Data.BarsRequest barsRequest;
				// Request x number of days back of data.
				if (instrument != null)
				{
					barsRequest = new NinjaTrader.Data.BarsRequest(instrument, DateTime.Now.AddDays(-lookBackPeriod), DateTime.Now);

					// If you wish to request x number of bars back instead you can use this signature:
					// barsRequest = new NinjaTrader.Data.BarsRequest(instrument, lookBackPeriod);
				}
				else
				{
					outputBox.Text = "Error: Invalid instrument";
					return null;
				}

				// Parameterize your request. We determine the interval via the selection from our interval selector.
				barsRequest.BarsPeriod = new NinjaTrader.Data.BarsPeriod { BarsPeriodType = intervalSelector.Interval.BarsPeriodType, Value = intervalSelector.Interval.Value };
				barsRequest.TradingHours = NinjaTrader.Data.TradingHours.Get("Default 24 x 7");
				// barsRequest.IsDividendAdjusted		= true;
				// barsRequest.IsResetOnNewTradingDay	= false;
				// barsRequest.IsSplitAdjusted			= true;
				// barsRequest.LookupPolicy				= LookupPolicies.Provider;
				// barsRequest.MergePolicy				= MergePolicy.DoNotMerge;

				// Attach event handler for real-time events if you want to process real-time data
				if (subscribeRealTimeData)
				{
					barsRequest.Update += OnBarUpdate;
					barsRequestSubscribed = true;
				}

				// Request the bars
				barsRequest.Request(new Action<NinjaTrader.Data.BarsRequest, ErrorCode, string>((bars, errorCode, errorMessage) =>
				{
					Dispatcher.InvokeAsync(new Action(() =>
					{
						if (errorCode != ErrorCode.NoError)
						{
							// Handle any errors in requesting bars here
							outputBox.Text = string.Format("Error on requesting bars: {0}, {1}", errorCode, errorMessage);
							return;
						}

						// Output the bars if requested
						if (outputType == AddOnFrameworkDisplay.requestData)
							PrintBarsRequest(bars);

						// If requesting real-time bars, but there are currently no connections
						if (barsRequestSubscribed)
						{
							lock (Connection.Connections)
								if (Connection.Connections.FirstOrDefault() == null)
								{
									outputBox.Text = string.Format("REALTIME BARS{0}Not connected.", Environment.NewLine);
								}
						}
					}));
				}));

				return barsRequest;
			}
			catch (Exception error)
			{
				// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
				outputBox.Text = "AddOnFramework - DoBarsRequest Exception: " + error.ToString();
				return null;
			}
		}

		// Output requested bars' OHLCV
		private void PrintBarsRequest(NinjaTrader.Data.BarsRequest bars)
		{
			outputBox.Text = "REQUESTED BARS: " + bars.Bars.Count;

			// Process returned bars here. Note: The last returned bar may be a currently in-progress bar
			for (int i = 0; i < bars.Bars.Count; i++)
			{
				// Do whatever you want with the bars
				outputBox.AppendText(string.Format("{0}Time: {1}{0}Open: {2}{0}High: {3}{0}Low: {4}{0}Close: {5}{0}Volume: {6}{0}",
					Environment.NewLine,
					bars.Bars.GetTime(i),
					bars.Bars.GetOpen(i),
					bars.Bars.GetHigh(i),
					bars.Bars.GetLow(i),
					bars.Bars.GetClose(i),
					bars.Bars.GetVolume(i)));
			}
		}
		#endregion

		#region Methods for Order Section
		// This method is fired when custom ATM properties are changed
		private void OnAtmCustomPropertiesChanged(object sender, NinjaTrader.Gui.NinjaScript.AtmStrategy.CustomPropertiesChangedEventArgs args)
		{
			// Adjust our TIF and Quantity selectors to the new ATM strategy values
			tifSelector.SelectedTif = args.NewTif;
			qudSelector.Value = args.NewQuantity;
		}

		// This method is fired as the status of an order changes
		private void OnOrderUpdate(object sender, OrderEventArgs e)
		{
			try
			{
				// Submit stop/target bracket orders for our framework managed entry
				if (frameworkEntryOrder != null && frameworkEntryOrder == e.Order)
				{
					// When our entry order fills, submit our profit target and stop loss orders
					if (e.OrderState == OrderState.Filled)
					{
						string oco = Guid.NewGuid().ToString("N");

						// Dispatcher.InvokeAsync() is needed for multi-threading considerations.
						Dispatcher.InvokeAsync(() =>
						{
							profitTarget = accountSelector.SelectedAccount.CreateOrder(Instrument, OrderAction.Sell, OrderType.Limit, TimeInForce.Day, 1, frameworkEntryOrder.AverageFillPrice + 10 * Instrument.MasterInstrument.TickSize, 0, oco, "AddOnFramework - Target", null);
							stopLoss = accountSelector.SelectedAccount.CreateOrder(Instrument, OrderAction.Sell, OrderType.StopMarket, TimeInForce.Day, 1, 0, frameworkEntryOrder.AverageFillPrice - 10 * Instrument.MasterInstrument.TickSize, oco, "AddOnFramework - Stop", null);
							accountSelector.SelectedAccount.Submit(new[] { profitTarget, stopLoss });
						});
					}
				}

				// Only display messages from this method if our output box is displaying this category of messages
				if (outputType != AddOnFrameworkDisplay.onOrderUpdate && outputType != AddOnFrameworkDisplay.frameworkManaged)
					return;

				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				Dispatcher.InvokeAsync(() =>
				{
					outputBox.AppendText(string.Format("{0}Instrument: {1}{0}OrderAction: {2}{0}OrderType: {3}{0}Quantity: {4}{0}LimitPrice: {5}{0}StopPrice: {6}{0}OrderState: {7}{0}Filled: {8}{0}AverageFillPrice: {9}{0}Name: {10}{0}OCO: {11}{0}TimeInForce: {12}{0}OrderID: {13}{0}Time: {14}{0}",
						Environment.NewLine,
						e.Order.Instrument.FullName,
						e.Order.OrderAction,
						e.Order.OrderType,
						e.Quantity,
						e.LimitPrice,
						e.StopPrice,
						e.OrderState,
						e.Filled,
						e.AverageFillPrice,
						e.Order.Name,
						e.Order.Oco,
						e.Order.TimeInForce,
						e.OrderId,
						e.Time));
				});
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnOrderUpdate Exception: " + error.ToString();
				});
			}
		}

		// This method is fired as new executions come in, an existing execution is amended (e.g. by the broker's back office), or an execution is removed (e.g. by the broker's back office)
		private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
		{
			try
			{
				// Only display messages from this method if our output box is displaying this category of messages
				if (outputType != AddOnFrameworkDisplay.onExecutionUpdate && outputType != AddOnFrameworkDisplay.buyMarket && outputType != AddOnFrameworkDisplay.sellMarket)
					return;

				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				Dispatcher.InvokeAsync(() =>
				{
					outputBox.AppendText(string.Format("{0}Instrument: {1}{0}Quantity: {2}{0}Price: {3}{0}Time: {4}{0}ExecutionID: {5}{0}Exchange: {6}{0}MarketPosition: {7}{0}Operation: {8}{0}OrderID: {9}{0}Name: {10}{0}Commission: {11}{0}Rate: {12}{0}",
						Environment.NewLine,
						e.Execution.Instrument.FullName,
						e.Quantity,
						e.Price,
						e.Time,
						e.ExecutionId,
						e.Exchange,
						e.MarketPosition,
						e.Operation,
						e.OrderId,
						e.Execution.Name,
						e.Execution.Commission,
						e.Execution.Rate));
				});
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnExecutionUpdate Exception: " + error.ToString();
				});
			}
		}

		// This method is fired as a position changes
		private void OnPositionUpdate(object sender, PositionEventArgs e)
		{
			try
			{
				// Only display messages from this method if our output box is displaying this category of messages
				if (outputType != AddOnFrameworkDisplay.onPositionUpdate)
					return;

				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				//                Dispatcher.InvokeAsync(() =>
				//                {
				//                    outputBox.AppendText(string.Format("{0}Instrument: {1}{0}MarketPosition: {2}{0}Quantity: {3}{0}AveragePrice: {4}{0}Operation: {5}{0}UnrealizedPnL: {6}{0}Connection: {7}{0}",
				//                        Environment.NewLine,
				//                        e.Position.Instrument.FullName,
				//                        e.MarketPosition,
				//                        e.Quantity,
				//                        e.AveragePrice,
				//                        e.Operation,
				//                        (e.Position.Instrument.MarketData.Last != null ? e.Position.GetProfitLoss(e.Position.Instrument.MarketData.Last.Price, PerformanceUnit.Currency).ToString() : "(unknown)"),
				//                        accountSelector.SelectedAccount.Connection.Options.Name));
				//                });
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnPositionUpdate Exception: " + error.ToString();
				});
			}
		}
		#endregion

		#region Methods for Misc Section
		// Connect to a connection
		private Connection Connect(string connectionName)
		{
			try
			{
				// Get the configured account connection
				ConnectOptions connectOptions = null;
				lock (NinjaTrader.Core.Globals.ConnectOptions)
					connectOptions = NinjaTrader.Core.Globals.ConnectOptions.FirstOrDefault(o => o.Name == connectionName);

				if (connectOptions == null)
				{
					outputType = AddOnFrameworkDisplay.connectKinetickEOD;
					outputBox.Text = "Could not connect. No connection found.";
					return null;
				}

				// If connection is not already connected, connect.
				lock (Connection.Connections)
					if (Connection.Connections.FirstOrDefault(c => c.Options.Name == connectionName) == null)
					{
						Connection connect = Connection.Connect(connectOptions);

						// Only return connection if successfully connected
						if (connect.Status == ConnectionStatus.Connected)
							return connect;
						else
							return null;
					}

				return null;
			}
			catch (Exception error)
			{
				outputType = AddOnFrameworkDisplay.connectKinetickEOD;

				// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
				outputBox.Text = "AddOnFramework - Connect Exception: " + error.ToString();
				return null;
			}
		}

		// This method is fired on connection status events
		private void OnConnectionStatusUpdate(object sender, ConnectionStatusEventArgs e)
		{
			try
			{
				// For multi-threading reasons, work with a copy of the EventArgs to prevent situations where the EventArgs may already be ahead of us while in the middle processing it.
				ConnectionStatusEventArgs eCopy = e;

				if (eCopy.Connection.Options.Name == "Kinetick – End Of Day (Free)")
				{
					// Switch button to be a connect button
					if (eCopy.Status == ConnectionStatus.Disconnected)
					{
						connection = null;
						Dispatcher.InvokeAsync(() =>
						{
							connectKinetickEODButton.Content = "Connect Kinetick EOD";
						});
					}
					// Switch button to be a disconnect button
					else if (eCopy.Status == ConnectionStatus.Connected)
					{
						if (connection == null)
							connection = eCopy.Connection;
						Dispatcher.InvokeAsync(() =>
						{
							connectKinetickEODButton.Content = "Disconnect Kinetick EOD";
						});
					}
				}

				if (outputType != AddOnFrameworkDisplay.onConnectionStatusUpdate)
					return;

				// If connection hits an error, report it
				if (eCopy.Error != ErrorCode.NoError)
				{
					Dispatcher.InvokeAsync(() =>
					{
						outputBox.AppendText(string.Format("{0}{1}. Error: {2} NativeError: {3}",
							Environment.NewLine,
							eCopy.Connection.Options.Name,
							eCopy.Error,
							eCopy.NativeError));
					});
				}

				Dispatcher.InvokeAsync(() =>
				{
					outputBox.AppendText(string.Format("{0}{1}. Status: {2}",
							Environment.NewLine,
							eCopy.Connection.Options.Name,
							eCopy.Status));
				});
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnConnectionStatusUpdate Exception: " + error.ToString();
				});
			}
		}

		// This method is fired on sim account reset events.
		// IMPORTANT: Be sure to recreate bar requests after a reset on the Playback connection
		private void OnSimulationAccountReset(object sender, EventArgs e)
		{
			try
			{
				if (outputType != AddOnFrameworkDisplay.onSimulationAccountReset)
					return;

				Account simAccount = (sender as Account);
				Dispatcher.InvokeAsync(() =>
				{
					outputBox.AppendText(string.Format("{0}'{1}' reset.",
						Environment.NewLine,
						simAccount.Name));
				});

				// If the account was reset due to a rewind/fast forward of the Playback connection
				if (simAccount != null && simAccount.Provider == Provider.Playback)
				{
					// We would need to redo our bars requests here if we are using them in Playback
				}
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnSimulationAccountReset Exception: " + error.ToString();
				});
			}
		}

		// This method is fired as new News events come in. Note: old News events are not provided as you have subscribed
		private void OnNews(object sender, NinjaTrader.Data.NewsEventArgs e)
		{
			try
			{
				if (outputType != AddOnFrameworkDisplay.onNews)
					return;

				Dispatcher.InvokeAsync(() =>
				{
					outputBox.AppendText(string.Format("{0}ID: {1}{0}NewsProvider: {2}{0}Headline: {3}{0}Text: {4}{0}Time: {5}{0}URL: {6}{0}",
						Environment.NewLine,
						e.Id,
						e.NewsProvider,
						e.Headline,
						e.Text,
						e.Time.ToString("yyyyMMddHHmmss"),
						(e.Url != null ? e.Url.AbsoluteUri : "(null)")));
				});

				// Maintain the news items
				newsItems.Update(e);
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "AddOnFramework - OnNews Exception: " + error.ToString();
				});
			}
		}
		#endregion

		// IInstrumentProvider member. Required if you want to use the instrument link mechanism on this window.
		//private NinjaTrader.Cbi.Instrument instrument; 
		public NinjaTrader.Cbi.Instrument Instrument
		{
			get { return instrument; }
			set
			{
				NinjaTrader.NinjaScript.NinjaScript.Log("Test: " + instrument, LogLevel.Information);
				// Unsubscribe to subscriptions to old instruments
				if (instrument != null)
				{
					marketData.Update -= OnMarketData;
					marketDepth.Update -= OnMarketDepth;
					fundamentalData.Update -= OnFundamentalData;

					marketData = null;
					marketDepth = null;
					fundamentalData = null;

					if (barsRequest != null)
					{
						if (barsRequestSubscribed)
						{
							barsRequest.Update -= OnBarUpdate;
							barsRequestSubscribed = false;
						}
						barsRequest = null;
					}
				}

				// Subscribe for the new instrument
				if (value != null)
				{
					// Create a market data snapshot and subscribe for updates
					marketData = new MarketData(value);
					marketData.Update += OnMarketData;

					// Create a market depth snapshot and subscribe for updates
					marketDepth = new MarketDepth<MarketDepthRow>(value);
					marketDepth.Update += OnMarketDepth;

					// Create a fundamental data snapshot and subscribe for updates
					fundamentalData = new FundamentalData(value);
					fundamentalData.Update += OnFundamentalData;
				}

				instrument = value;
				if (instrumentSelector != null)
					instrumentSelector.Instrument = value;

				// Update the tab header name on AddOnFramework to be the same name as the new instrument
				RefreshHeader();

				// Send instrument to other windows linked to the same color
				PropagateInstrumentChange(value);
			}

		}
		// IIntervalProvider member. Required if you want to use the interval linker mechanism on this window. No functionality has been linked to the interval linker in this sample.
		public NinjaTrader.Data.BarsPeriod BarsPeriod
		{ get; set; }

		// NTTabPage member. Required to determine the text for the tab header name
		protected override string GetHeaderPart(string variable)
		{
			switch (variable)
			{
				case "@INSTRUMENT":
				return Instrument == null ? "New Tab" : Instrument.MasterInstrument.Name;
				case "@INSTRUMENT_FULL":
				return Instrument == null ? "New Tab" : Instrument.FullName;
			}
			return variable;
		}

		// Called by TabControl when tab is being removed or window is closed
		public override void Cleanup()
		{
			// Unsubscribe and cleanup any remaining resources we may still have open
			Account.AccountStatusUpdate -= OnAccountStatusUpdate;
			Account.SimulationAccountReset -= OnSimulationAccountReset;
			Connection.ConnectionStatusUpdate -= OnConnectionStatusUpdate;
			if (accountSelector.SelectedAccount != null)
			{
				accountSelector.SelectedAccount.AccountItemUpdate -= OnAccountItemUpdate;
				accountSelector.SelectedAccount.ExecutionUpdate -= OnExecutionUpdate;
				accountSelector.SelectedAccount.OrderUpdate -= OnOrderUpdate;
				accountSelector.SelectedAccount.PositionUpdate -= OnPositionUpdate;
			}

			// a call to base.Cleanup() will loop through the visual tree looking for all ICleanable children
			// i.e., AccountSelector, AtmStrategySelector, InstrumentSelector, IntervalSelector, TifSelector,
			// as well as unregister any link control events
			base.Cleanup();

			if (marketData != null)
				marketData.Update -= OnMarketData;
			if (marketDepth != null)
				marketDepth.Update -= OnMarketDepth;
			if (barsRequest != null && barsRequestSubscribed)
				barsRequest.Update -= OnBarUpdate;
			if (fundamentalData != null)
				fundamentalData.Update -= OnFundamentalData;
		}

		// NTTabPage member. Required for restoring elements from workspace
		protected override void Restore(XElement element)
		{
			if (element == null)
				return;

			// Restore the previously selected account
			XElement accountElement = element.Element("Account");
			if (accountSelector != null && accountElement != null)
				accountSelector.DesiredAccount = accountElement.Value;

			// Restore the previously selected instrument
			XElement instrumentElement = element.Element("Instrument");
			if (instrumentElement != null && !string.IsNullOrEmpty(instrumentElement.Value))
				Instrument = NinjaTrader.Cbi.Instrument.GetInstrument(instrumentElement.Value);
		}

		// NTTabPage member. Required for storing elements to workspace
		protected override void Save(XElement element)
		{
			if (element == null)
				return;

			// Save the currently selected account
			if (accountSelector != null && !string.IsNullOrEmpty(accountSelector.DesiredAccount))
				element.Add(new XElement("Account") { Value = accountSelector.DesiredAccount });

			// Save the currently selected instrument
			if (Instrument != null)
				element.Add(new XElement("Instrument") { Value = Instrument.FullName });
		}
	}
}

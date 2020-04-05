#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
#endregion

//This namespace holds GUI items and is required.
namespace NinjaTrader.Gui.NinjaScript
{
	// NT creates an instance of each class derived from "AddOnBase" and call OnWindowCreated/OnWindowDestroyed for every instance and every NTWindow which is created or destroyed...
	public class AddOnFramework : AddOnBase
	{
		private NTMenuItem addOnFrameworkMenuItem;
		private NTMenuItem existingMenuItemInControlCenter;

		// Same as other NS objects. However there's a difference: this event could be called in any thread
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Example AddOn demonstrating some of the framework's capabilities";
				Name = "AddOn Framework";
			}
		}

		// Will be called as a new NTWindow is created. It will be called in the thread of that window
		protected override void OnWindowCreated(Window window)
		{
			// We want to place our AddOn in the Control Center's menus
			ControlCenter cc = window as ControlCenter;
			if (cc == null)
				return;

			/* Determine we want to place our AddOn in the Control Center's "New" menu
            Other menus can be accessed via the control's "Automation ID". For example: toolsMenuItem, workspacesMenuItem, connectionsMenuItem, helpMenuItem. */
			existingMenuItemInControlCenter = cc.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
			if (existingMenuItemInControlCenter == null)
				return;

			// 'Header' sets the name of our AddOn seen in the menu structure
			addOnFrameworkMenuItem = new NTMenuItem { Header = "AddOn Framework", Style = Application.Current.TryFindResource("MainMenuItem") as Style };

			// Add our AddOn into the "New" menu
			existingMenuItemInControlCenter.Items.Add(addOnFrameworkMenuItem);

			// Subscribe to the event for when the user presses our AddOn's menu item
			addOnFrameworkMenuItem.Click += OnMenuItemClick;
		}

		// Will be called as a new NTWindow is destroyed. It will be called in the thread of that window
		protected override void OnWindowDestroyed(Window window)
		{
			if (addOnFrameworkMenuItem != null && window is ControlCenter)
			{
				if (existingMenuItemInControlCenter != null && existingMenuItemInControlCenter.Items.Contains(addOnFrameworkMenuItem))
					existingMenuItemInControlCenter.Items.Remove(addOnFrameworkMenuItem);

				addOnFrameworkMenuItem.Click -= OnMenuItemClick;
				addOnFrameworkMenuItem = null;
			}
		}

		// Open our AddOn's window when the menu item is clicked on
		private void OnMenuItemClick(object sender, RoutedEventArgs e)
		{
			Core.Globals.RandomDispatcher.BeginInvoke(new Action(() => new AddOnFrameworkWindow().Show()));
		}
	}

	/* Class which implements Tools.INTTabFactory must be created and set as an attached property for TabControl
    in order to use tab page add/remove/move/duplicate functionality */
	public class AddOnFrameworkWindowFactory : INTTabFactory
	{
		// INTTabFactory member. Required to create parent window
		public NTWindow CreateParentWindow()
		{
			return new AddOnFrameworkWindow();
		}

		// INTTabFactory member. Required to create tabs
		public NTTabPage CreateTabPage(string typeName, bool isTrue)
		{
			return new NinjaTraderAddOnProject.AddOnPage();
		}
	}

	/* This is where we define our AddOn window. The actual content is contained inside the tabs of the window defined in public class AddOnFrameworkTab below.
        We have to create a new window class which inherits from Tools.NTWindow for styling and implements IWorkspacePersistence interface for ability to save/restore from workspaces. */
	public class AddOnFrameworkWindow : NTWindow, IWorkspacePersistence
	{
		public AddOnFrameworkWindow()
		{
			// set Caption property (not Title), since Title is managed internally to properly combine selected Tab Header and Caption for display in the windows taskbar
			// This is the name displayed in the top-left of the window
			Caption = "AddOn Framework";

			// Set the default dimensions of the window
			Width = 1085;
			Height = 900;

			// TabControl should be created for window content if tab features are wanted
			TabControl tc = new TabControl();

			// Attached properties defined in TabControlManager class should be set to achieve tab moving, adding/removing tabs
			TabControlManager.SetIsMovable(tc, true);
			TabControlManager.SetCanAddTabs(tc, true);
			TabControlManager.SetCanRemoveTabs(tc, true);

			// if ability to add new tabs is desired, TabControl has to have attached property "Factory" set.
			TabControlManager.SetFactory(tc, new AddOnFrameworkWindowFactory());
			Content = tc;

			/* In order to have link buttons functionality, tab control items must be derived from Tools.NTTabPage
            They can be added using extention method AddNTTabPage(NTTabPage page) */
			tc.AddNTTabPage(new NinjaTraderAddOnProject.AddOnPage());

			// WorkspaceOptions property must be set
			Loaded += (o, e) =>
			{
				if (WorkspaceOptions == null)
					WorkspaceOptions = new WorkspaceOptions("AddOnFramework-" + Guid.NewGuid().ToString("N"), this);
			};
		}

		// IWorkspacePersistence member. Required for restoring window from workspace
		public void Restore(XDocument document, XElement element)
		{
			if (MainTabControl != null)
				MainTabControl.RestoreFromXElement(element);
		}

		// IWorkspacePersistence member. Required for saving window to workspace
		public void Save(XDocument document, XElement element)
		{
			if (MainTabControl != null)
				MainTabControl.SaveToXElement(element);
		}

		// IWorkspacePersistence member
		public WorkspaceOptions WorkspaceOptions
		{ get; set; }
	}
}
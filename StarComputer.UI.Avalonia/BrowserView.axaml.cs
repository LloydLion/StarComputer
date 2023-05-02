using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using StarComputer.Common.Abstractions.Plugins;
using System.ComponentModel;
using System.Reactive.Linq;

namespace StarComputer.UI.Avalonia
{
	public partial class BrowserView : UserControl
	{
		private readonly Dictionary<PluginAvaloniaBrowser, Decorator> browserPoolDecorators = new();


		private BrowserViewModel Context => (BrowserViewModel)DataContext!;


		public BrowserView()
		{
			InitializeComponent();

#if !DEBUG
			devButton.IsVisible = false;
#endif

			if (Design.IsDesignMode == false)
			{
				Initialized += OnInitialized;
				Background = null;
			}
			else
			{
				tabs.Items = new DesignTab[]
				{
					new DesignTab("Tab 1"),
					new DesignTab("Tab 2"),
					new DesignTab("Tab 3"),
					new DesignTab("Tab 4")
				};
			}
		}


		private void OnInitialized(object? sender, EventArgs e)
		{
			foreach (var browser in Context.BrowsersToVisualize)
			{
				var decorator = new Decorator
				{
					IsVisible = false, IsEnabled = false
				};

				browser.UseDecorator(decorator);
				browserPoolDecorators.Add(browser, decorator);
			}

			browserPool.Children.AddRange(browserPoolDecorators.Values);

			addMenu.Items = Context.AvailablePluginsCasted.Select(CastAddMenuItem).ToArray();


			devButton.Click += (_, _) =>
			{
				var mainPlugin = Context.ActiveTab?.MainWindowPlugin;
				if (mainPlugin is not null)
				{
					Context.BrowsersToVisualize[mainPlugin.GetDomain()].SetDevtoolsVisibility(status: true);
				}
			};

			tabs.PropertyChanged += async (_, e) =>
			{
				if (e.Property == SelectingItemsControl.SelectedItemProperty)
				{
					if (tabs.SelectedItem is BrowserViewModel.BrowserTab newSelection)
					{
						await Task.Yield();
						if (Context.CombinationChoose is not null)
						{
							var cache = Context.CombinationChoose;
							Context.CombinationChoose = null;
							Context.OpenCombined(cache, newSelection.MainWindowPlugin);
							tabs.SelectedItem = Context.ActiveTab;
						}
						else
						{
							Context.OpenTab(newSelection);
						}
					}
				}
			};

			Context.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(BrowserViewModel.ActiveTab))
				{
					var value = Context.ActiveTab;
					if (value is null) return;

					updateMainFrame(value.MainWindowPlugin);

					if (value.SecondWindowPlugin is not null)
						updateSecondFrame(value.SecondWindowPlugin);


					value.PropertyChanged += activeTabPropertyChangedSubscriber;
					value.PropertyChanging += activeTabPropertyChangingSubscriber;

					tabs.SelectedItem = Context.ActiveTab; //Will changed only if not equal
				}

				if (e.PropertyName == nameof(BrowserViewModel.LeftSidebarActivePlugin))
				{
					if (Context.LeftSidebarActivePlugin is not null)
						updateLeftSidebarFrame(Context.LeftSidebarActivePlugin);

					ResetLeftMainGrid();
				}

				if (e.PropertyName == nameof(BrowserViewModel.RightSidebarActivePlugin))
				{
					if (Context.RightSidebarActivePlugin is not null)
						updateRightSidebarFrame(Context.RightSidebarActivePlugin);

					ResetRightMainGrid();
				}

				if (e.PropertyName == nameof(BrowserViewModel.AvailablePluginsCasted))
				{
					addMenu.Items = Context.AvailablePluginsCasted.Select(CastAddMenuItem).ToArray();
				}
			};

			Context.PropertyChanging += (_, e) =>
			{
				if (e.PropertyName == nameof(BrowserViewModel.ActiveTab))
				{
					var value = Context.ActiveTab;
					if (value is null) return;

					hidePlugin(value.MainWindowPlugin);

					if (value.SecondWindowPlugin is not null)
					{
						hidePlugin(value.SecondWindowPlugin);
						ResetInnerGrid();
					}


					value.PropertyChanged -= activeTabPropertyChangedSubscriber;
					value.PropertyChanging -= activeTabPropertyChangingSubscriber;
				}

				if (e.PropertyName == nameof(BrowserViewModel.LeftSidebarActivePlugin))
				{
					if (Context.LeftSidebarActivePlugin is not null)
						hidePlugin(Context.LeftSidebarActivePlugin);
				}

				if (e.PropertyName == nameof(BrowserViewModel.RightSidebarActivePlugin))
				{
					if (Context.RightSidebarActivePlugin is not null)
						hidePlugin(Context.RightSidebarActivePlugin);
				}
			};



			void activeTabPropertyChangingSubscriber(object? _, PropertyChangingEventArgs e)
			{
				if (e.PropertyName == nameof(BrowserViewModel.BrowserTab.MainWindowPlugin) && Context.ActiveTab?.MainWindowPlugin is not null)
					hidePlugin(Context.ActiveTab.MainWindowPlugin);

				if (e.PropertyName == nameof(BrowserViewModel.BrowserTab.SecondWindowPlugin) && Context.ActiveTab?.SecondWindowPlugin is not null)
				{
					hidePlugin(Context.ActiveTab.SecondWindowPlugin);
					ResetInnerGrid();
				}
			}

			void activeTabPropertyChangedSubscriber(object? _, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == nameof(BrowserViewModel.BrowserTab.MainWindowPlugin) && Context.ActiveTab?.MainWindowPlugin is not null)
					updateMainFrame(Context.ActiveTab.MainWindowPlugin);

				if (e.PropertyName == nameof(BrowserViewModel.BrowserTab.SecondWindowPlugin) && Context.ActiveTab?.SecondWindowPlugin is not null)
					updateSecondFrame(Context.ActiveTab.SecondWindowPlugin);
			}

			void updateMainFrame(IPlugin plugin)
			{
				if (mainFrame.Child is not null)
					throw new InvalidOperationException("Main frame is not empty");
				var browser = Context.BrowsersToVisualize[plugin.GetDomain()];
				browser.UseDecorator(mainFrame);
			}

			void updateSecondFrame(IPlugin plugin)
			{
				if (secondFrame.Child is not null)
					throw new InvalidOperationException("Second frame is not empty");
				var browser = Context.BrowsersToVisualize[plugin.GetDomain()];
				browser.UseDecorator(secondFrame);

				ResetInnerGrid();
			}

			void updateLeftSidebarFrame(IPlugin plugin)
			{
				if (leftSidebarFrame.Child is not null)
					throw new InvalidOperationException("Left sidebar frame is not empty");
				var browser = Context.BrowsersToVisualize[plugin.GetDomain()];
				browser.UseDecorator(leftSidebarFrame);
			}

			void updateRightSidebarFrame(IPlugin plugin)
			{
				if (rightSidebarFrame.Child is not null)
					throw new InvalidOperationException("Right sidebar frame is not empty");
				var browser = Context.BrowsersToVisualize[plugin.GetDomain()];
				browser.UseDecorator(rightSidebarFrame);
			}

			void hidePlugin(IPlugin plugin)
			{
				var browser = Context.BrowsersToVisualize[plugin.GetDomain()];
				browser.UseDecorator(browserPoolDecorators[browser]);
				browser.SetDevtoolsVisibility(status: false);
			}
		}

		private void ResetInnerGrid()
		{
			innerGrid.RowDefinitions[3].Height = GridLength.Auto;
		}

		private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
		{
			var control = (Control)sender!;
			var point = e.GetCurrentPoint(this);
			if (point.Properties.IsRightButtonPressed)
			{
				Context.CombinationChoose = null;

				e.Handled = true;

				var flyout = control.GetValue(FlyoutBase.AttachedFlyoutProperty);
				flyout?.ShowAt(control, showAtPointer: true);
			}
		}

		private void CloseLeftSidebar(object? sender, RoutedEventArgs e)
		{
			if (Context.LeftSidebarActivePlugin is not null)
				Context.ClosePlugin(Context.LeftSidebarActivePlugin);
		}

		private void CloseRightSidebar(object? sender, RoutedEventArgs e)
		{
			if (Context.RightSidebarActivePlugin is not null)
				Context.ClosePlugin(Context.RightSidebarActivePlugin);
		}

		private void ResetLeftMainGrid()
		{
			mainGrid.ColumnDefinitions[0].Width = GridLength.Auto;
			mainGrid.ColumnDefinitions[1].Width = GridLength.Auto;
			mainGrid.ColumnDefinitions[2].Width = GridLength.Star;
		}

		private void ResetRightMainGrid()
		{
			mainGrid.ColumnDefinitions[2].Width = GridLength.Star;
			mainGrid.ColumnDefinitions[3].Width = GridLength.Auto;
			mainGrid.ColumnDefinitions[4].Width = GridLength.Auto;
		}

		private MenuItem CastAddMenuItem(BrowserViewModel.PluginOpenViewModel vm)
		{
			return new MenuItem()
			{
				Header = vm.Title,
				CommandParameter = vm.TargetPlugin,
				Command = vm.OpenPlugin
			};
		}

		private record DesignTab(string Title);
	}
}

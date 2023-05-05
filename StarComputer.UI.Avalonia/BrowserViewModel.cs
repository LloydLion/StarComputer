using DynamicData;
using Microsoft.Extensions.Localization;
using StarComputer.Common.Abstractions.Plugins;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace StarComputer.UI.Avalonia
{
	public class BrowserViewModel : ViewModelBase, INotifyPropertyChanging
	{
		private readonly IBrowserCollection browsers;
		private readonly IPluginStore plugins;
		private BrowserTab? activeTab;
		private IPlugin? leftSidebarActivePlugin;
		private IPlugin? rightSidebarActivePlugin;
		private IPlugin? combinationChoose;

		private readonly ICommand closeCommand;
		private readonly ICommand combineCommand;
		private readonly ICommand openInRightSidebarCommand;
		private readonly ICommand openInLeftSidebarCommand;
		private readonly ICommand openCommand;


		public event PropertyChangingEventHandler? PropertyChanging;


		public BrowserViewModel(IBrowserCollection browsers, IPluginStore plugins, IStringLocalizer<BrowserView> localizer)
		{
			this.browsers = browsers;
			this.plugins = plugins;


			PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(LeftSidebarActivePlugin) || e.PropertyName == nameof(RightSidebarActivePlugin))
					RaisePropertyChanged(nameof(AvailablePlugins));

				if (e.PropertyName == nameof(AvailablePlugins))
					RaisePropertyChanged(nameof(AvailablePluginsCasted));
			};

			Tabs.CollectionChanged += (_, e) =>
			{
				RaisePropertyChanged(nameof(AvailablePlugins));
			};


			closeCommand = new DelegateCommand<BrowserTab>((model) =>
			{
				CloseTab(model);
			});

			combineCommand = new DelegateCommand<BrowserTab>((model) =>
			{
				combinationChoose = model.MainWindowPlugin;
			});

			openInRightSidebarCommand = new DelegateCommand<BrowserTab>((model) =>
			{
				OpenRightSidebar(model.MainWindowPlugin);
			});

			openInLeftSidebarCommand = new DelegateCommand<BrowserTab>((model) =>
			{
				OpenLeftSidebar(model.MainWindowPlugin);
			});

			openCommand = new DelegateCommand<IPlugin>(Open);

			Localization = new LocalizationModel(localizer);
		}


		public IBrowserCollection BrowsersToVisualize => browsers;

		public IEnumerable<IPlugin> AvailablePlugins => plugins.Values.Where(s => !(Tabs.Any(tab => tab.MainWindowPlugin == s || tab.SecondWindowPlugin == s) || LeftSidebarActivePlugin == s || RightSidebarActivePlugin == s));

		public IEnumerable<PluginOpenViewModel> AvailablePluginsCasted => AvailablePlugins.Select(s => new PluginOpenViewModel(this, s));

		public ObservableCollection<BrowserTab> Tabs { get; } = new();

		public BrowserTab? ActiveTab { get => activeTab; set => RaiseAndSetIfChanged(ref activeTab, value); }

		public IPlugin? LeftSidebarActivePlugin { get => leftSidebarActivePlugin; set => RaiseAndSetIfChanged(ref leftSidebarActivePlugin, value); }

		public IPlugin? RightSidebarActivePlugin { get => rightSidebarActivePlugin; set => RaiseAndSetIfChanged(ref rightSidebarActivePlugin, value); }

		public IPlugin? CombinationChoose { get => combinationChoose; set => RaiseAndSetIfChanged(ref combinationChoose, value); }

		public LocalizationModel Localization { get; }


		public void OpenTab(BrowserTab tab)
		{
			if (Tabs.Contains(tab) == false)
				throw new ArgumentException("No this tab in tab collection", nameof(tab));

			ActiveTab = tab;
		}

		public void Open(IPlugin plugin)
		{
			var oldTab = Tabs.FirstOrDefault(tab => tab.MainWindowPlugin == plugin || tab.SecondWindowPlugin == plugin);
			if (oldTab is not null)
			{
				ActiveTab = oldTab;
				return;
			}

			var tab = new BrowserTab(this, plugin);

			if (LeftSidebarActivePlugin == plugin)
				LeftSidebarActivePlugin = null;
			else if (RightSidebarActivePlugin == plugin)
				RightSidebarActivePlugin = null;

			Tabs.Add(tab);
			ActiveTab = tab;
		}

		public void OpenCombined(IPlugin plugin1, IPlugin plugin2)
		{
			if (plugin1 == plugin2)
			{
				Open(plugin1);
			}
			else
			{
				ClosePlugin(plugin2);
				Open(plugin1);
				ActiveTab!.SecondWindowPlugin = plugin2;
			}
		}

		public void HideCurrent() => ActiveTab = null;

		public void OpenRightSidebar(IPlugin? plugin)
		{
			if (plugin is not null)
				ClosePlugin(plugin);
			RightSidebarActivePlugin = plugin;
		}

		public void OpenLeftSidebar(IPlugin? plugin)
		{
			if (plugin is not null)
				ClosePlugin(plugin);
			LeftSidebarActivePlugin = plugin;
		}

		public void CloseTab(BrowserTab tab)
		{
			if (ActiveTab == tab)
				HideCurrent();
			Tabs.Remove(tab);
		}

		public void ClosePlugin(IPlugin plugin)
		{
			var oldTab = Tabs.FirstOrDefault(tab => tab.MainWindowPlugin == plugin || tab.SecondWindowPlugin == plugin);
			if (oldTab is not null)
			{
				if (oldTab.MainWindowPlugin == plugin)
				{
					if (oldTab.SecondWindowPlugin is null)
					{
						CloseTab(oldTab);
					}
					else
					{
						oldTab.MainWindowPlugin = oldTab.SecondWindowPlugin;
						oldTab.SecondWindowPlugin = null;
					}
				}
				else
				{
					oldTab.SecondWindowPlugin = null;
				}
			}


			if (LeftSidebarActivePlugin == plugin)
				LeftSidebarActivePlugin = null;
			else if (RightSidebarActivePlugin == plugin)
				RightSidebarActivePlugin = null;
		}

		private new void RaiseAndSetIfChanged<TValue>(ref TValue variable, TValue newValue, [CallerMemberName] string nameOfCaller = "Name of caller")
		{
			if (EqualityComparer<TValue>.Default.Equals(variable, newValue) == false)
			{
				PropertyChanging?.Invoke(this, new(nameOfCaller));
				variable = newValue;
				RaisePropertyChanged(nameOfCaller);
			}
		}


		public class PluginOpenViewModel
		{
			private readonly BrowserViewModel owner;
			private readonly IPlugin targetPlugin;


			public PluginOpenViewModel(BrowserViewModel owner, IPlugin targetPlugin)
			{
				this.owner = owner;
				this.targetPlugin = targetPlugin;
			}


			public string Title => targetPlugin.GetDomain();

			public ICommand OpenPlugin => owner.openCommand;

			public IPlugin TargetPlugin => targetPlugin;
		}

		public class BrowserTab : INotifyPropertyChanged, INotifyPropertyChanging
		{
			private readonly BrowserViewModel owner;
			private IPlugin mainWindowPlugin;
			private IPlugin? secondWindowPlugin;


			public BrowserTab(BrowserViewModel owner, IPlugin mainWindowPlugin, IPlugin? secondWindowPlugin = null)
			{
				this.owner = owner;
				this.mainWindowPlugin = mainWindowPlugin;
				this.secondWindowPlugin = secondWindowPlugin;
				Title = GenerateTitle();
			}


			public string Title { get; private set; }

			public IPlugin MainWindowPlugin { get => mainWindowPlugin; set => SetAndNotify(ref mainWindowPlugin, value); }

			public IPlugin? SecondWindowPlugin { get => secondWindowPlugin; set => SetAndNotify(ref secondWindowPlugin, value); }

			public ICommand CloseCommand => owner.closeCommand;

			public ICommand CombineCommand => owner.combineCommand;

			public ICommand OpenInRightSidebarCommand => owner.openInRightSidebarCommand;

			public ICommand OpenInLeftSidebarCommand => owner.openInLeftSidebarCommand;

			public LocalizationModel Localization => owner.Localization;


			public event PropertyChangedEventHandler? PropertyChanged;

			public event PropertyChangingEventHandler? PropertyChanging;


			private void SetAndNotify<T>(ref T variable, T newValue, [CallerMemberName] string nameOfCaller = "name of caller")
			{
				if (Equals(variable, newValue) == false)
				{
					PropertyChanging?.Invoke(this, new(nameOfCaller));
					PropertyChanging?.Invoke(this, new(nameof(Title)));

					variable = newValue;
					PropertyChanged?.Invoke(this, new(nameOfCaller));

					Title = GenerateTitle();
					PropertyChanged?.Invoke(this, new(nameof(Title)));

				}
			}

			private string GenerateTitle()
			{
				var title = $"{MainWindowPlugin.GetDomain().Domain}";
				if (SecondWindowPlugin is not null)
					title += $" | {SecondWindowPlugin.GetDomain().Domain}";
				return title;
			}
		}

		private class DelegateCommand<TInput> : ICommand where TInput : class
		{
			private readonly Action<TInput> action;


			public event EventHandler? CanExecuteChanged
			{ add { } remove { } }


			public DelegateCommand(Action<TInput> action)
			{
				this.action = action;
			}


			public bool CanExecute(object? parameter) => true;

			public void Execute(object? parameter) => action(parameter as TInput ?? throw new ArgumentException($"Parameter should be {typeof(TInput)} type", nameof(parameter)));
		}

		public class LocalizationModel
		{
			private readonly IStringLocalizer localizer;


			public LocalizationModel(IStringLocalizer localizer)
			{
				this.localizer = localizer;
			}


			public string CloseMenuItemHeader => localizer[nameof(CloseMenuItemHeader)];

			public string OpenInRightSidebarMenuItemHeader => localizer[nameof(OpenInRightSidebarMenuItemHeader)];

			public string OpenInLeftSidebarMenuItemHeader => localizer[nameof(OpenInLeftSidebarMenuItemHeader)];

			public string CombineMenuItemHeader => localizer[nameof(CombineMenuItemHeader)];
		}
	}
}

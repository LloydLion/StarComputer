using StarComputer.UI.Avalonia;

namespace StarComputer.Server.UI.Avalonia
{
	public class ServerViewModel : ViewModelBase
	{
		public ServerViewModel(BrowserViewModel browser, ServerControlViewModel serverControl, PluginSelectorViewModel pluginSelector, ServerStatusBarViewModel statusBar)
		{
			Browser = browser;
			ServerControl = serverControl;
			PluginSelector = pluginSelector;
			StatusBar = statusBar;

			ServerControl.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(ServerControlViewModel.IsListening))
				{
					pluginSelector.SwitchPlugin(null);
					RaisePropertyChanged(nameof(IsListening));
				}
			};
		}


		public bool IsListening => ServerControl.IsListening;


		public BrowserViewModel Browser { get; }

		public ServerControlViewModel ServerControl { get; }

		public PluginSelectorViewModel PluginSelector { get; }

		public ServerStatusBarViewModel StatusBar { get; }
	}
}

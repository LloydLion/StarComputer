using StarComputer.UI.Avalonia;

namespace StarComputer.Client.UI.Avalonia
{
	public class ClientViewModel : ViewModelBase
	{
		public ClientViewModel(BrowserViewModel browser, ConnectionViewModel connection, PluginSelectorViewModel pluginSelector)
		{
			Browser = browser;
			Connection = connection;
			PluginSelector = pluginSelector;

			Connection.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(ConnectionViewModel.IsConnected))
				{
					pluginSelector.SwitchPlugin(null);
					RaisePropertyChanged(nameof(IsConnected));
				}
			};
		}


		public bool IsConnected => Connection.IsConnected;


		public BrowserViewModel Browser { get; }

		public ConnectionViewModel Connection { get; }

		public PluginSelectorViewModel PluginSelector { get; }
	}
}

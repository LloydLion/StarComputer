using StarComputer.UI.Avalonia;

namespace StarComputer.Server.UI.Avalonia
{
	public class ServerViewModel : ViewModelBase
	{
		public ServerViewModel(BrowserViewModel browser, ListenViewModel listen, PluginSelectorViewModel pluginSelector)
		{
			Browser = browser;
			Listen = listen;
			PluginSelector = pluginSelector;

			Listen.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(ListenViewModel.IsListening))
				{
					pluginSelector.SwitchPlugin(null);
					RaisePropertyChanged(nameof(IsListening));
				}
			};
		}


		public bool IsListening => Listen.IsListening;


		public BrowserViewModel Browser { get; }

		public ListenViewModel Listen { get; }

		public PluginSelectorViewModel PluginSelector { get; }
	}
}

using StarComputer.UI.Avalonia;

namespace StarComputer.Server.UI.Avalonia
{
	public class ServerViewModel : ViewModelBase
	{
		public ServerViewModel(BrowserViewModel browser, ServerControlViewModel serverControl, ServerStatusBarViewModel statusBar)
		{
			Browser = browser;
			ServerControl = serverControl;
			StatusBar = statusBar;

			ServerControl.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(ServerControlViewModel.IsListening))
				{
					RaisePropertyChanged(nameof(IsListening));
				}
			};
		}


		public bool IsListening => ServerControl.IsListening;


		public BrowserViewModel Browser { get; }

		public ServerControlViewModel ServerControl { get; }

		public ServerStatusBarViewModel StatusBar { get; }
	}
}

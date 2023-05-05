using Microsoft.Extensions.Localization;
using StarComputer.UI.Avalonia;

namespace StarComputer.Server.UI.Avalonia
{
	public class ServerViewModel : ViewModelBase
	{
		public ServerViewModel(BrowserViewModel browser, ServerControlViewModel serverControl, ServerStatusBarViewModel statusBar, IStringLocalizer<ServerView> localizer)
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

			Localization = new LocalizationModel(localizer);
		}


		public bool IsListening => ServerControl.IsListening;


		public BrowserViewModel Browser { get; }

		public ServerControlViewModel ServerControl { get; }

		public ServerStatusBarViewModel StatusBar { get; }

		public LocalizationModel Localization { get; }


		public class LocalizationModel
		{
			private readonly IStringLocalizer localizer;


			public LocalizationModel(IStringLocalizer localizer)
			{
				this.localizer = localizer;
			}


			public string WindowMenuItemHeader => localizer[nameof(WindowMenuItemHeader)];

			public string OpenServerStatusControlMenuItemHeader => localizer[nameof(OpenServerStatusControlMenuItemHeader)];
		}
	}
}

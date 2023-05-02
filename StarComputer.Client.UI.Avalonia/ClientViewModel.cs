using StarComputer.Client.Abstractions;
using StarComputer.UI.Avalonia;
using Avalonia.Threading;
using Avalonia.Controls;

namespace StarComputer.Client.UI.Avalonia
{
	public class ClientViewModel : ViewModelBase
	{
		private readonly IClient client;


		public ClientViewModel(Window owner, BrowserViewModel browser, ClientConnectionMenuViewModel connectionMenu, ClientStatusBarViewModel statusBar, IClient client)
		{
			Browser = browser;
			ConnectionMenu = connectionMenu;
			StatusBar = statusBar;
			this.client = client;

			client.ConnectionStatusChanged += (_, _) => Dispatcher.UIThread.Post(() =>
			{
				RaisePropertyChanged(nameof(IsConnected));
			}, DispatcherPriority.Send);
		}


		public bool IsConnected => client.IsConnected;


		public BrowserViewModel Browser { get; }

		public ClientConnectionMenuViewModel ConnectionMenu { get; }

		public ClientStatusBarViewModel StatusBar { get; }
	}
}

using Avalonia.Threading;
using StarComputer.Client.Abstractions;
using StarComputer.Server.Abstractions;
using StarComputer.UI.Avalonia;
using System;

namespace StarComputer.Server.UI.Avalonia
{
	public class ServerStatusBarViewModel : ViewModelBase
	{
		private readonly IServer server;


		public ServerStatusBarViewModel(IServer server)
		{
			this.server = server;
			server.ListeningStatusChanged += OnServerListeningStatusChanged;
		}


		public bool IsListening => server.IsListening;

		public bool IsNotListening => server.IsListening == false;

		public ServerConfiguration Configuration => server.GetConfiguration();


		private void OnServerListeningStatusChanged(object? sender, EventArgs e)
		{
			Dispatcher.UIThread.Post(() =>
			{
				RaisePropertyChanged(nameof(IsListening));
				RaisePropertyChanged(nameof(IsNotListening));
			}, DispatcherPriority.Send);
		}
	}
}

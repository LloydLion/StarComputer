using Avalonia.Threading;
using StarComputer.Client.Abstractions;
using StarComputer.UI.Avalonia;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public class ClientStatusBarViewModel : ViewModelBase
	{
		private readonly IClient client;


		public ClientStatusBarViewModel(IClient client)
		{
			this.client = client;
			client.ConnectionStatusChanged += OnClientConnectionStatusChanged;
		}


		public bool IsConnected => client.IsConnected;

		public bool IsNotConnected => client.IsConnected == false;

		public ConnectionConfiguration? ConnectionConfiguration => IsConnected ? client.GetConnectionConfiguration() : null;

		public ClientConfiguration Configuration => client.GetConfiguration();


		private void OnClientConnectionStatusChanged(object? sender, EventArgs e)
		{
			Dispatcher.UIThread.Post(() =>
			{
				RaisePropertyChanged(nameof(IsConnected));
				RaisePropertyChanged(nameof(IsNotConnected));
				RaisePropertyChanged(nameof(ConnectionConfiguration));
			}, DispatcherPriority.Send);
		}
	}
}

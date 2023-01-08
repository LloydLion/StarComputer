using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;

namespace StarComputer.Client.UI.Avalonia
{
	public partial class ConnectionView : UserControl
	{
		private ConnectionViewModel Context => (ConnectionViewModel)DataContext!;


		public ConnectionView()
		{
			InitializeComponent();
			if (Design.IsDesignMode == false)
				Initialized += OnViewInitialized;
		}


		private void OnViewInitialized(object? sender, EventArgs e)
		{
			connectButton.Click += OnConnectButtonClick;
			//sendButton.Click += OnSendButtonClick;
			disconnectButton.Click += OnDisconnectButtonClick;

			connectButton.IsEnabled = Context.CanConnect;
			disconnectButton.IsEnabled = Context.IsConnected;
			Context.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(ConnectionViewModel.CanConnect))
					connectButton.IsEnabled = Context.CanConnect;
				if (e.PropertyName == nameof(ConnectionViewModel.IsConnected))
					disconnectButton.IsEnabled = Context.IsConnected;
			};
		}

		private void OnDisconnectButtonClick(object? sender, RoutedEventArgs e)
		{
			Context.Disconnect();
		}

		private async void OnConnectButtonClick(object? sender, RoutedEventArgs e)
		{
			await ConnectToServer();
		}

		private async ValueTask ConnectToServer()
		{
			IsEnabled = false;
			await Context.ConnectToServerAsync();
			IsEnabled = true;
		}
	}
}

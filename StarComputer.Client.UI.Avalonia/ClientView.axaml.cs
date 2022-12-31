using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public partial class ClientView : UserControl
	{
		private ClientViewModel Context => (ClientViewModel)DataContext!;


		public ClientView()
		{
			InitializeComponent();

			Initialized += OnViewInitialized;
		}


		private void OnViewInitialized(object? sender, EventArgs e)
		{
			connectButton.Click += OnConnectButtonClick;
			sendButton.Click += OnSendButtonClick;

			connectButton.IsEnabled = Context.CanConnect;
			Context.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(ClientViewModel.CanConnect))
					connectButton.IsEnabled = Context.CanConnect;
			};

			lineInput.PropertyChanged += (_, e) =>
			{
				if (e.Property == TextBox.TextProperty)
					sendButton.IsEnabled = string.IsNullOrWhiteSpace(lineInput.Text) == false;
			};

			lineInput.KeyDown += OnLineInputKeyDown;
		}

		private void OnLineInputKeyDown(object? sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				var line = lineInput.Text;
				Context.SendLine(line);
				lineInput.Text = "";
			}			
		}

		private void OnSendButtonClick(object? sender, RoutedEventArgs e)
		{
			var line = lineInput.Text;
			Context.SendLine(line);
			lineInput.Text = "";
		}

		private void OnConnectButtonClick(object? sender, RoutedEventArgs e)
		{
			Context.ConnectToServer();
		}
	}
}

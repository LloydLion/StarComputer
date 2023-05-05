using Avalonia.Threading;
using Microsoft.Extensions.Localization;
using StarComputer.Client.Abstractions;
using StarComputer.UI.Avalonia;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public class ClientStatusBarViewModel : ViewModelBase
	{
		private readonly IClient client;


		public ClientStatusBarViewModel(IClient client, IStringLocalizer<ClientStatusBarView> localizer)
		{
			this.client = client;
			client.ConnectionStatusChanged += OnClientConnectionStatusChanged;
			Localization = new LocalizationModel(localizer);
		}


		public bool IsConnected => client.IsConnected;

		public bool IsNotConnected => client.IsConnected == false;

		public ConnectionConfiguration? ConnectionConfiguration => IsConnected ? client.GetConnectionConfiguration() : null;

		public ClientConfiguration Configuration => client.GetConfiguration();

		public LocalizationModel Localization { get; }


		private void OnClientConnectionStatusChanged(object? sender, EventArgs e)
		{
			Dispatcher.UIThread.Post(() =>
			{
				RaisePropertyChanged(nameof(IsConnected));
				RaisePropertyChanged(nameof(IsNotConnected));
				RaisePropertyChanged(nameof(ConnectionConfiguration));
			}, DispatcherPriority.Send);
		}


		public class LocalizationModel
		{
			private readonly IStringLocalizer localizer;


			public LocalizationModel(IStringLocalizer localizer)
			{
				this.localizer = localizer;
			}


			public string ConnectedToLabel => localizer[nameof(ConnectedToLabel)];

			public string NoConnectionLabel => localizer[nameof(NoConnectionLabel)];

			public string UsingInterfaceLabel => localizer[nameof(UsingInterfaceLabel)];

			public string LoggedAsLabel => localizer[nameof(LoggedAsLabel)];
		}
	}
}

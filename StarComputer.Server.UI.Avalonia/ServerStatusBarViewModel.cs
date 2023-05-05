using Avalonia.Threading;
using Microsoft.Extensions.Localization;
using StarComputer.Server.Abstractions;
using StarComputer.UI.Avalonia;
using System;

namespace StarComputer.Server.UI.Avalonia
{
	public class ServerStatusBarViewModel : ViewModelBase
	{
		private readonly IServer server;


		public ServerStatusBarViewModel(IServer server, IStringLocalizer<ServerStatusBarView> localizer)
		{
			this.server = server;
			server.ListeningStatusChanged += OnServerListeningStatusChanged;

			Localization = new LocalizationModel(localizer);
		}


		public bool IsListening => server.IsListening;

		public bool IsNotListening => server.IsListening == false;

		public ServerConfiguration Configuration => server.GetConfiguration();

		public LocalizationModel Localization { get; }


		private void OnServerListeningStatusChanged(object? sender, EventArgs e)
		{
			Dispatcher.UIThread.Post(() =>
			{
				RaisePropertyChanged(nameof(IsListening));
				RaisePropertyChanged(nameof(IsNotListening));
			}, DispatcherPriority.Send);
		}

		public class LocalizationModel
		{
			private readonly IStringLocalizer localizer;


			public LocalizationModel(IStringLocalizer localizer)
			{
				this.localizer = localizer;
			}


			public string ServerClosedLabel => localizer[nameof(ServerClosedLabel)];

			public string ServerListeningOn => localizer[nameof(ServerListeningOn)];
		}
	}
}

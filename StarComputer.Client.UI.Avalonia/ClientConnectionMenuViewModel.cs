using Avalonia.Threading;
using StarComputer.Client.Abstractions;
using StarComputer.UI.Avalonia;
using System.Threading.Tasks;
using System;
using Avalonia.Controls;
using Microsoft.Extensions.Localization;

namespace StarComputer.Client.UI.Avalonia
{
	public class ClientConnectionMenuViewModel : ViewModelBase
	{
		private readonly Window owner;
		private readonly ConnectionDialogViewModel connectionDialogViewModel;
		private readonly IClient client;


		public ClientConnectionMenuViewModel(IClient client, Window owner, ConnectionDialogViewModel connectionDialogViewModel, IStringLocalizer<ClientConnectionMenuView> localizer)
		{
			this.client = client;
			this.owner = owner;
			this.connectionDialogViewModel = connectionDialogViewModel;
			client.ConnectionStatusChanged += (_, _) => Dispatcher.UIThread.Post(() => RaisePropertyChanged(nameof(IsConnected)), DispatcherPriority.Send);

			Localization = new LocalizationModel(localizer);
		}


		public bool IsConnected => client.IsConnected;

		public LocalizationModel Localization { get; }


		public async ValueTask TryConnectToNewServerAsync()
		{
			try
			{
				var dialog = new ConnectionDialogView()
				{
					DataContext = connectionDialogViewModel
				};

				var result = await dialog.ShowDialog<ConnectionConfiguration?>(owner);
				if (result is null) return;

				if (client.IsConnected)
					await client.CloseAsync();

				await client.ConnectAsync(result.Value);
			}
			catch (Exception ex)
			{
				await ErrorDialogView.ShowAsync(ex.ToString(), owner);
			}
		}

		public async Task DisconnectAsync()
		{
			try
			{
				await client.CloseAsync();
			}
			catch (Exception ex)
			{
				await ErrorDialogView.ShowAsync(ex.ToString(), owner);
			}
		}


		public class LocalizationModel
		{
			private readonly IStringLocalizer localizer;


			public LocalizationModel(IStringLocalizer localizer)
			{
				this.localizer = localizer;
			}


			public string ConnectionMenuHeader => localizer[nameof(ConnectionMenuHeader)];

			public string CloseConnectionMenuHeader => localizer[nameof(CloseConnectionMenuHeader)];

			public string OpenNewConnectionMenuHeader => localizer[nameof(OpenNewConnectionMenuHeader)];
		}
	}
}

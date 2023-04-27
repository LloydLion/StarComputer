using Avalonia.Controls;
using Avalonia.Threading;
using StarComputer.Server.Abstractions;
using StarComputer.UI.Avalonia;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace StarComputer.Server.UI.Avalonia
{
	public class ServerControlViewModel : ViewModelBase
	{
		private readonly IServer server;
		private readonly Window owner;


		public ServerControlViewModel(IServer server, Window owner)
		{
			this.server = server;
			this.owner = owner;
			server.ListeningStatusChanged += (sender, e) => Dispatcher.UIThread.Post(() => RaisePropertyChanged(nameof(IsListening)), DispatcherPriority.Send);

			server.ClientConnected += (sender, e) => Dispatcher.UIThread.Post(() => Clients.Add(new ServerClientUIDTO(e.Client.ConnectionInformation.Login, e.Client.ConnectionInformation.CallbackUri.ToString())));
			server.ClientDisconnected += (sender, e) => Dispatcher.UIThread.Post(() => Clients.Remove(new ServerClientUIDTO(e.Client.ConnectionInformation.Login, e.Client.ConnectionInformation.CallbackUri.ToString())));

			PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(IsListening))
					RaisePropertyChanged(nameof(CanStartListen));
			};
		}


		public bool IsListening => server.IsListening;

		public bool CanStartListen => server.IsCanStartListen;

		public ObservableCollection<ServerClientUIDTO> Clients { get; } = new();


		public async ValueTask ListenAsync()
		{
			try
			{
				await server.ListenAsync();
			}
			catch (Exception ex)
			{
				await ErrorDialogView.ShowAsync(ex.ToString(), owner);
			}
		}


		public record ServerClientUIDTO(string Login, string Endpoint);
	}
}

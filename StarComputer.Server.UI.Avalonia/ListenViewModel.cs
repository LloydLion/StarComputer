using Avalonia.Threading;
using StarComputer.Server.Abstractions;
using StarComputer.UI.Avalonia;
using System;
using System.Threading.Tasks;

namespace StarComputer.Server.UI.Avalonia
{
	public class ListenViewModel : ViewModelBase
	{
		private readonly IServer server;
		private string errorMessage = string.Empty;


		public ListenViewModel(IServer server)
		{
			this.server = server;

			server.ListeningStatusChanged += () => Dispatcher.UIThread.Post(() => RaisePropertyChanged(nameof(IsListening)), DispatcherPriority.Send);

			PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(IsListening))
					RaisePropertyChanged(nameof(CanStartListen));
			};
		}


		public bool IsListening => server.IsListening;

		public bool CanStartListen => !IsListening;

		public string ErrorMessage { get => errorMessage; private set => RaiseAndSetIfChanged(ref errorMessage, value); }


		public async ValueTask ListenAsync()
		{
			try
			{
				ErrorMessage = string.Empty;

				await server.ListenAsync();

				ErrorMessage = string.Empty;
			}
			catch (Exception ex)
			{
				ErrorMessage = ex.ToString();
			}
		}
	}
}

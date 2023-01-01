using Avalonia.Threading;
using StarComputer.Client.Abstractions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace StarComputer.Client.UI.Avalonia
{
	public class ClientViewModel : ViewModelBase
	{
		private readonly IClient client;
		private readonly AvaloniaBasedConsoleUIContext uiContext;
		private IPEndPoint? parsedConnectionEndPoint;
		private string? connectionEndPoint;
		private string? login;
		private string? serverPassword;
		private bool canConnect = false;
		private bool isValidConnectionEndPoint = false;
		private string errorMessage = "";


		public ClientViewModel(IClient client, AvaloniaBasedConsoleUIContext uiContext)
		{
			PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(ConnectionEndPoint))
					isValidConnectionEndPoint = ConnectionEndPoint is not null && IPEndPoint.TryParse(ConnectionEndPoint, out parsedConnectionEndPoint);

				if (e.PropertyName == nameof(Login) || e.PropertyName == nameof(ConnectionEndPoint) || e.PropertyName == nameof(ServerPassword) || e.PropertyName == nameof(IsConnected))
					CanConnect =
						IsConnected == false &&
						string.IsNullOrWhiteSpace(Login) == false &&
						string.IsNullOrWhiteSpace(ServerPassword) == false &&
						string.IsNullOrWhiteSpace(ConnectionEndPoint) == false &&
						IsValidConnectionEndPoint;
			};

			uiContext.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(AvaloniaBasedConsoleUIContext.OutputContent))
					RaisePropertyChanged(nameof(OutputContent));
			};

			client.ConnectionStatusChanged += () => Dispatcher.UIThread.Post(() => RaisePropertyChanged(nameof(IsConnected)), DispatcherPriority.Send);

			this.client = client;
			this.uiContext = uiContext;
		}


		public string? ConnectionEndPoint { get => connectionEndPoint; set => RaiseAndSetIfChanged(ref connectionEndPoint, value); }

		public bool IsValidConnectionEndPoint { get => isValidConnectionEndPoint; set => RaiseAndSetIfChanged(ref isValidConnectionEndPoint, value); }

		public string? Login { get => login; set => RaiseAndSetIfChanged(ref login, value); }

		public string? ServerPassword { get => serverPassword; set => RaiseAndSetIfChanged(ref serverPassword, value); }

		public bool CanConnect { get => canConnect; private set => RaiseAndSetIfChanged(ref canConnect, value); }

		public string OutputContent => uiContext.OutputContent;

		public string ConnectionErrorMessage { get => errorMessage; set => RaiseAndSetIfChanged(ref errorMessage, value); }

		public bool IsConnected => client.IsConnected;


		public async ValueTask ConnectToServerAsync()
		{
			try
			{
				ConnectionErrorMessage = string.Empty;

				if (parsedConnectionEndPoint is null || Login is null || ServerPassword is null)
					throw new InvalidOperationException("Login, endpoint or password is null. Fill it before connect");

				await client.ConnectAsync(new(parsedConnectionEndPoint, ServerPassword, Login));

				ConnectionErrorMessage = string.Empty;
			}
			catch (Exception ex)
			{
				ConnectionErrorMessage = ex.ToString();
			}
		}

		public void SendLine(string line)
		{
			uiContext.SendNewLine(line);
		}
	}
}

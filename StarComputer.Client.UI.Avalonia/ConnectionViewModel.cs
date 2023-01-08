using Avalonia.Threading;
using Microsoft.Extensions.Options;
using StarComputer.Client.Abstractions;
using StarComputer.UI.Avalonia;
using System;
using System.Net;
using System.Threading.Tasks;

namespace StarComputer.Client.UI.Avalonia
{
	public class ConnectionViewModel : ViewModelBase
	{
		private readonly IClient client;
		private IPEndPoint? parsedConnectionEndPoint;
		private string? connectionEndPoint;
		private string? login;
		private string? serverPassword;
		private bool canConnect = false;
		private bool isValidConnectionEndPoint = false;
		private string errorMessage = "";
		private bool isConnectionLoginChangable;
		private bool isConnectionDataChangable;


		public ConnectionViewModel(IClient client, IOptions<Options> options)
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

				if (e.PropertyName == nameof(IsConnected))
				{
					IsConnectionDataChangable = !IsConnected && !options.Value.IsConnectionDataLocked;
					IsConnectionLoginChangable = !IsConnected && !options.Value.IsConnectionLoginLocked;
				}
			};

			client.ConnectionStatusChanged += () => Dispatcher.UIThread.Post(() => RaisePropertyChanged(nameof(IsConnected)), DispatcherPriority.Send);

			this.client = client;

			IsConnectionDataChangable = !options.Value.IsConnectionDataLocked;
			IsConnectionLoginChangable = !options.Value.IsConnectionLoginLocked;
			if (options.Value.InitialConnectionInformation is not null)
			{
				var initialData = options.Value.InitialConnectionInformation.Value;
				ConnectionEndPoint = initialData.EndPoint.ToString();
				Login = initialData.Login;
				ServerPassword = initialData.ServerPassword;
			}
		}


		public void Disconnect()
		{
			client.GetServerAgent().Disconnect();
		}

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


		public string? ConnectionEndPoint { get => connectionEndPoint; set => RaiseAndSetIfChanged(ref connectionEndPoint, value); }

		public bool IsValidConnectionEndPoint { get => isValidConnectionEndPoint; private set => RaiseAndSetIfChanged(ref isValidConnectionEndPoint, value); }

		public string? Login { get => login; set => RaiseAndSetIfChanged(ref login, value); }

		public string? ServerPassword { get => serverPassword; set => RaiseAndSetIfChanged(ref serverPassword, value); }

		public bool CanConnect { get => canConnect; private set => RaiseAndSetIfChanged(ref canConnect, value); }

		public string ConnectionErrorMessage { get => errorMessage; private set => RaiseAndSetIfChanged(ref errorMessage, value); }

		public bool IsConnected => client.IsConnected;

		public bool IsConnectionDataChangable { get => isConnectionDataChangable; private set => RaiseAndSetIfChanged(ref isConnectionDataChangable, value); }

		public bool IsConnectionLoginChangable { get => isConnectionLoginChangable; private set => RaiseAndSetIfChanged(ref isConnectionLoginChangable, value); }


		public class Options
		{
			public bool IsConnectionDataLocked { get; set; }

			public bool IsConnectionLoginLocked { get; set; }

			public ConnectionConfiguration? InitialConnectionInformation { get; set; }
		}
	}
}

using ReactiveUI;
using StarComputer.Client.Abstractions;
using System;
using System.ComponentModel;
using System.Net;
using System.Reactive;

namespace StarComputer.Client.UI.Avalonia
{
	public class ClientViewModel : ViewModelBase
	{
		private readonly IClient? client;
		private readonly AvaloniaBasedConsoleUIContext? uiContext;
		private string? connectionEndPoint;
		private IPEndPoint? parsedConnectionEndPoint;
		private string? login;
		private bool canConnect = false;
		private string? serverPassword;
		private bool isValidConnectionEndPoint;


		public ClientViewModel(IClient? client = null, AvaloniaBasedConsoleUIContext? uiContext = null)
		{
			PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(ConnectionEndPoint))
					isValidConnectionEndPoint = ConnectionEndPoint is not null && IPEndPoint.TryParse(ConnectionEndPoint, out parsedConnectionEndPoint);

				if (e.PropertyName == nameof(Login) || e.PropertyName == nameof(ConnectionEndPoint) || e.PropertyName == nameof(ServerPassword))
					CanConnect =
						string.IsNullOrWhiteSpace(Login) == false &&
						string.IsNullOrWhiteSpace(ServerPassword) == false &&
						string.IsNullOrWhiteSpace(ConnectionEndPoint) == false &&
						IsValidConnectionEndPoint;
			};

			if (uiContext is not null)
				uiContext.PropertyChanged += (sender, e) =>
				{
					if (e.PropertyName == nameof(AvaloniaBasedConsoleUIContext.OutputContent))
						RaisePropertyChanged(nameof(OutputContent));
				};

			this.client = client;
			this.uiContext = uiContext;
		}


		public string? ConnectionEndPoint { get => connectionEndPoint; set => RaiseAndSetIfChanged(ref connectionEndPoint, value); }

		public bool IsValidConnectionEndPoint { get => isValidConnectionEndPoint; set => RaiseAndSetIfChanged(ref isValidConnectionEndPoint, value); }

		public string? Login { get => login; set => RaiseAndSetIfChanged(ref login, value); }

		public string? ServerPassword { get => serverPassword; set => RaiseAndSetIfChanged(ref serverPassword, value); }

		public bool CanConnect { get => canConnect; private set => RaiseAndSetIfChanged(ref canConnect, value); }

		public string OutputContent => uiContext?.OutputContent ?? "Content from plugin output";


		public void ConnectToServer()
		{
			if (client is not null && parsedConnectionEndPoint is not null && Login is not null && ServerPassword is not null)
				client.Connect(new(parsedConnectionEndPoint, ServerPassword, Login));
		}

		public void SendLine(string line)
		{
			uiContext?.SendNewLine(line);
		}
	}
}

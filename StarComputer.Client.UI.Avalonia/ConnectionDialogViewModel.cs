using Microsoft.Extensions.Options;
using StarComputer.Client.Abstractions;
using StarComputer.UI.Avalonia;
using System.Net;
using System.Threading.Tasks;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public class ConnectionDialogViewModel : ViewModelBase
	{
		private IPEndPoint? parsedConnectionEndPoint;
		private string? connectionEndPoint;
		private string? login;
		private string? serverPassword;
		private bool isValidConnectionEndPoint = false;
		private bool isConnectionLoginChangable;
		private bool isConnectionDataChangable;
		private bool canContinue;


		public ConnectionDialogViewModel(IOptions<Options> options)
		{
			PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(ConnectionEndPoint))
					isValidConnectionEndPoint = ConnectionEndPoint is not null && IPEndPoint.TryParse(ConnectionEndPoint, out parsedConnectionEndPoint);

				if (e.PropertyName == nameof(Login) || e.PropertyName == nameof(ConnectionEndPoint) || e.PropertyName == nameof(ServerPassword))
					CanContinue =
						string.IsNullOrWhiteSpace(Login) == false &&
						string.IsNullOrWhiteSpace(ServerPassword) == false &&
						string.IsNullOrWhiteSpace(ConnectionEndPoint) == false &&
						isValidConnectionEndPoint;
			};


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


		public string? ConnectionEndPoint { get => connectionEndPoint; set => RaiseAndSetIfChanged(ref connectionEndPoint, value); }

		public string? Login { get => login; set => RaiseAndSetIfChanged(ref login, value); }

		public string? ServerPassword { get => serverPassword; set => RaiseAndSetIfChanged(ref serverPassword, value); }

		public bool IsConnectionDataChangable { get => isConnectionDataChangable; private set => RaiseAndSetIfChanged(ref isConnectionDataChangable, value); }

		public bool IsConnectionLoginChangable { get => isConnectionLoginChangable; private set => RaiseAndSetIfChanged(ref isConnectionLoginChangable, value); }

		public bool CanContinue { get => canContinue; private set => RaiseAndSetIfChanged(ref canContinue, value); }


		public ConnectionConfiguration FormConnectionConfiguration()
		{
			if (CanContinue == false)
				throw new InvalidOperationException("Enable to form connection configuration from invalid data");
			return new ConnectionConfiguration(parsedConnectionEndPoint!, serverPassword!, login!);
		}


		public class Options
		{
			public bool IsConnectionDataLocked { get; set; }

			public bool IsConnectionLoginLocked { get; set; }

			public ConnectionConfiguration? InitialConnectionInformation { get; set; }
		}
	}
}

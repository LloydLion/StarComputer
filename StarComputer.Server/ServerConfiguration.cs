using StarComputer.Shared;
using System.Net;
using System.Reflection;

namespace StarComputer.Server
{
	public class ServerConfiguration
	{
#if !DEBUG
		private bool isPasswordSetted = false;
#endif
		private string serverPassword = "DEBUG PASSWORD";


		public int ConnectionPort { get; set; } = StaticInformation.ConnectionPort;

		public int ClientConnectTimeout { get; set; } = StaticInformation.ClientConnectTimeout;

		public PortRange OperationsPortRange { get; set; } = StaticInformation.OperationsPortRange;

		public IPAddress Interface { get; set; } = IPAddress.Parse("localhost");

		public int MaxPendingConnectionQueue { get; set; } = 10;

		public Version TargetProtocolVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version ?? throw new NullReferenceException();

		public string ServerPassword
		{
			get => serverPassword;

			set
			{
#if !DEBUG
				isPasswordSetted = true;
				serverPassword = value;
#endif
			}
		}


		public void Validate()
		{
			if (ConnectionPort <= 0 || ConnectionPort > ushort.MaxValue)
				throw new ArgumentException($"Port must be in 1 - {ushort.MaxValue} range");

			if (MaxPendingConnectionQueue <= 0)
				throw new ArgumentException($"MaxPendingConnectionQueue must be positive");

#if !DEBUG
			if (isPasswordSetted == false)
				throw new ArgumentException($"Password must be setted in non debug mode");
#endif
		}
	}
}
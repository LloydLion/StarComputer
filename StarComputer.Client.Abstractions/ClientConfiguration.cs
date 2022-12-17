using StarComputer.Common;
using StarComputer.Common.Abstractions;
using System.Reflection;

namespace StarComputer.Client.Abstractions
{
	public class ClientConfiguration
	{
		public Version TargetProtocolVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version ?? throw new NullReferenceException();

		public int ServerReconnectionPrepareTimeout { get; set; } = StaticInformation.ServerReconnectionPrepareTimeout;
	}
}

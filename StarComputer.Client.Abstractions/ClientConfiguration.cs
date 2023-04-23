using StarComputer.Common;
using StarComputer.Common.Abstractions;
using System.Net;
using System.Reflection;

namespace StarComputer.Client.Abstractions
{
	public class ClientConfiguration
	{
		public Version TargetProtocolVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version ?? throw new NullReferenceException();

		public IPEndPoint Interface { get; set; } = IPEndPoint.Parse("127.0.0.1:" + StaticInformation.OperationPort);

		public string ClientHttpAddressTemplate { get; set; } = StaticInformation.ClientHttpAddressTemplate;


		public string ConstructClientHttpAddress() => ClientHttpAddressTemplate.Replace("{Interface}", Interface.ToString());
	}
}

using StarComputer.Common.Abstractions;
using System.Net;

namespace StarComputer.Client.Abstractions
{
	public record struct ConnectionConfiguration(IPEndPoint EndPoint, string ServerPassword, string Login)
	{
		public string ServerHttpAddressTemplate { get; set; } = StaticInformation.ServerHttpAddressTemplate;


		public string ConstructServerHttpAddress() => ServerHttpAddressTemplate.Replace("{Interface}", EndPoint.ToString());
	}
}

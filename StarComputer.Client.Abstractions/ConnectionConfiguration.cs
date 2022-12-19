using System.Net;

namespace StarComputer.Client.Abstractions
{
	public record struct ConnectionConfiguration(IPEndPoint EndPoint, string ServerPassword, string Login);
}

using System.Net;

namespace StarComputer.Shared.Connection
{
	public record struct ClientConnectionInformation(string Login, IPEndPoint OriginalEndPoint);
}

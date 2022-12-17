using System.Net;

namespace StarComputer.Common.Abstractions.Connection
{
	public record struct ClientConnectionInformation(string Login, IPEndPoint OriginalEndPoint);
}

using System.Net;

namespace StarComputer.Server
{
	internal record struct ClientConnectionInformation(string Login, IPEndPoint EndPoint);
}

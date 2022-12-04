using System.Net;

namespace StarComputer.Server
{
	internal record struct ClientApprovalInformation(string ComputerName, IPEndPoint EndPoint);
}

using System.Net;

namespace StarComputer.Client
{
	internal interface IClient
	{
		public void Connect(IPEndPoint endPoint, string serverPassword, string login);
	}
}

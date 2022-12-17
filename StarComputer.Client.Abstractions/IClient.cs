using System.Net;

namespace StarComputer.Client.Abstractions
{
	public interface IClient
	{
		public void Connect(IPEndPoint endPoint, string serverPassword, string login);
	}
}

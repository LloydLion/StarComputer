using StarComputer.Client.Abstractions;

namespace StarComputer.Client
{
	public class ClientProtocolEnvironment : IClientProtocolEnviroment
	{
		public ClientProtocolEnvironment(IClient client)
		{
			Client = client;
		}


		public IClient Client { get; }
	}
}

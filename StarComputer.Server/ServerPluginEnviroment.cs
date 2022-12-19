namespace StarComputer.Server.Abstractions
{
	public class ServerProtocolEnvironment : IServerProtocolEnvironment
	{
		public ServerProtocolEnvironment(IServer server)
		{
			Server = server;
		}


		public IServer Server { get; }
	}
}

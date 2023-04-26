namespace StarComputer.Server.Abstractions
{
	public class ServerClientStatusChangedEventArgs : EventArgs
	{
		public ServerSideClient Client { get; }


		public ServerClientStatusChangedEventArgs(ServerSideClient client)
		{
			Client = client;
		}
	}
}

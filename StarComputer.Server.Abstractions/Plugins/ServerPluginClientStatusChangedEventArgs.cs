namespace StarComputer.Server.Abstractions.Plugins
{
	public class ServerPluginClientStatusChangedEventArgs : EventArgs
	{
		public ServerSidePluginClient Client { get; }


		public ServerPluginClientStatusChangedEventArgs(ServerSidePluginClient client)
		{
			Client = client;
		}
	}
}

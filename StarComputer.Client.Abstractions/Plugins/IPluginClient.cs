using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins.Protocol;

namespace StarComputer.Client.Abstractions.Plugins
{
	public interface IPluginClient
	{
		public bool IsConnected { get; }


		public event EventHandler ClientConnected;

		public event EventHandler ClientDisconnected;


		public IPluginRemoteAgent GetServerAgent();

		public ConnectionConfiguration GetConnectionConfiguration();
	}
}

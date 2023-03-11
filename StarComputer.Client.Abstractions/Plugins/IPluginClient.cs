using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins.Protocol;

namespace StarComputer.Client.Abstractions.Plugins
{
	public interface IPluginClient
	{
		public bool IsConnected { get; }


		public event Action ClientConnected;

		public event Action ClientDisconnected;


		public IPluginRemoteAgent GetServerAgent();

		public ConnectionConfiguration GetConnectionConfiguration();
	}
}

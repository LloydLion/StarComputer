using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;
using System.Net;

namespace StarComputer.Client.Abstractions
{
	public interface IClient
	{
		public void Connect(ConnectionConfiguration connectionConfiguration);

		public IRemoteProtocolAgent GetServerAgent();

		public ConnectionConfiguration GetConnectionConfiguration();

		void MainLoop(IPluginStore plugins);
	}
}

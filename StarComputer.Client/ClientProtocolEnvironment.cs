using StarComputer.Client.Abstractions;
using StarComputer.Client.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Loading;

namespace StarComputer.Client
{
	public class ClientProtocolEnvironment : IClientProtocolEnviroment
	{
		public ClientProtocolEnvironment(IClient client, PluginLoadingProto loadingProto)
		{
			Client = new PluginClient(client, loadingProto.Domain);
		}


		public IPluginClient Client { get; }
	}
}

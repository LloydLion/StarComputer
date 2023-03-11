using StarComputer.Common.Abstractions.Plugins.Loading;

namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IPluginInitializer
	{
		public IEnumerable<IPlugin> InitializePlugins(IEnumerable<PluginLoadingProto> plugins);
	}
}

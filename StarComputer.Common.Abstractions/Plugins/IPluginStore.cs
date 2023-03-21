using StarComputer.Common.Abstractions.Plugins.Loading;

namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IPluginStore : IReadOnlyDictionary<string, IPlugin>
	{
		public bool IsInitialized { get; }


		public void InitializeStore(IPluginLoader loader, IPluginInitializer initializer);
	}
}

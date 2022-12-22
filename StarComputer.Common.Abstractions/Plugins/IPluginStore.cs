namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IPluginStore : IReadOnlyDictionary<string, IPlugin>
	{
		public bool IsInitialized { get; }


		public ValueTask InitializeStoreAsync(IPluginLoader loader);

		public void InitalizePlugins(IPluginInitializer initializer);
	}
}

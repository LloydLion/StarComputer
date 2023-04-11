namespace StarComputer.Common.Abstractions.Plugins.Persistence
{
	public interface IPluginPersistenceServiceProvider
	{
		public IPluginPersistenceService Provide(PluginDomain domain);
	}
}

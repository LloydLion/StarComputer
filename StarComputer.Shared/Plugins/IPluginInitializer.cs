namespace StarComputer.Shared.Plugins
{
	public interface IPluginInitializer
	{
		public void InitializePlugins(IEnumerable<IPlugin> plugins);
	}
}

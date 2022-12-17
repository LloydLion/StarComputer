namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IPluginInitializer
	{
		public void InitializePlugins(IEnumerable<IPlugin> plugins);
	}
}

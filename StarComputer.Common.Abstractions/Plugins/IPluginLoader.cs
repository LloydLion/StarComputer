namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IPluginLoader
	{
		public ValueTask<IEnumerable<IPlugin>> LoadPluginsAsync();
	}
}

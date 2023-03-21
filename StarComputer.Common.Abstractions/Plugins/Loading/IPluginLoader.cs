namespace StarComputer.Common.Abstractions.Plugins.Loading
{
	public interface IPluginLoader
	{
		public IEnumerable<PluginLoadingProto> LoadPlugins();
	}
}

using StarComputer.Common.Abstractions.Plugins.Loading;

namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IUIContextFactory<out TUI> where TUI : IUIContext
	{
		public TUI CreateContext(PluginLoadingProto proto);
	}
}

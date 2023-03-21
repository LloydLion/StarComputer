using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.UI.Avalonia
{
	public interface IBrowserCollection : IEnumerable<PluginAvaloniaBrowser>
	{
		public bool IsBrowserEnvironmentInitialized { get; }


		public PluginAvaloniaBrowser this[PluginDomain plugin] { get; }


		public bool IsBrowserCreatedFor(PluginDomain plugin);

		public void OnBrowserEnvironmentInitialized(EventHandler eventHandler);
	}
}

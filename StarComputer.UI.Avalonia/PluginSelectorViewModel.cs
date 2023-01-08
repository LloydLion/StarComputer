using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.UI.Avalonia
{
	public class PluginSelectorViewModel : ViewModelBase
	{
		private readonly HTMLUIManager uiManager;


		public PluginSelectorViewModel(HTMLUIManager uiManager, IPluginStore plugins)
		{
			Plugins = plugins.Select(s => new VisualPlugin(s.Value));
			this.uiManager = uiManager;
		}


		public IEnumerable<object> Plugins { get; }


		public void SwitchPlugin(object? plugin)
		{
			if (plugin is null)
				uiManager.SwitchPlugin(null);
			else if (plugin is VisualPlugin vp)
				uiManager.SwitchPlugin(vp.Plugin);
			else throw new ArgumentException("Invalid type of plugin", nameof(plugin));
		}


		private record VisualPlugin(IPlugin Plugin)
		{
			public override string ToString()
			{
				return $"{Plugin.Domain} [{Plugin.GetType().FullName}|{Plugin.Version}]";
			}
		}
	}
}

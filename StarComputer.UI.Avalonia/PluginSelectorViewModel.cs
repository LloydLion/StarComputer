using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.UI.Avalonia
{
	public class PluginSelectorViewModel : ViewModelBase
	{
		private readonly IPluginChangeHandler handler;


		public PluginSelectorViewModel(IPluginChangeHandler handler, IPluginStore plugins)
		{
			Plugins = plugins.Select(s => new VisualPlugin(s.Value));
			this.handler = handler;
		}


		public IEnumerable<object> Plugins { get; }


		public void SwitchPlugin(object? plugin)
		{
			if (plugin is null)
				handler.SwitchPlugin(null);
			else if (plugin is VisualPlugin vp)
				handler.SwitchPlugin(vp.Plugin);
			else throw new ArgumentException("Invalid type of plugin", nameof(plugin));
		}


		private record VisualPlugin(IPlugin Plugin)
		{
			public override string ToString()
			{
				return $"{Plugin.GetDomain()} [{Plugin.GetType().FullName}|{Plugin.Version}]";
			}
		}
	}

	public interface IPluginChangeHandler
	{
		public void SwitchPlugin(IPlugin? plugin);
	}
}

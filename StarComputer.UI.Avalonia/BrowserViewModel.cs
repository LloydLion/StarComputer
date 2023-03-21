using Avalonia.Threading;
using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.UI.Avalonia
{
	public class BrowserViewModel : ViewModelBase, IPluginChangeHandler
	{
		private readonly IBrowserCollection browsers;
		private IPlugin? activePlugin;


		public IPlugin? ActivePlugin { get => activePlugin; private set => RaiseAndSetIfChanged(ref activePlugin, value); }

		public IBrowserCollection BrowsersToVisualize => browsers;


		public BrowserViewModel(IBrowserCollection browsers)
		{
			this.browsers = browsers;
		}


		public void SwitchPlugin(IPlugin? plugin)
		{
			ActivePlugin = plugin;
		}
	}
}

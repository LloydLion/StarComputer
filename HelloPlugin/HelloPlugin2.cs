using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using StarComputer.PluginDevelopmentKit;

namespace HelloPlugin
{
	[Plugin("Hello2")]
	public class HelloPlugin2 : PluginBase
	{
		private readonly IHTMLUIContext ui;


		public HelloPlugin2(IProtocolEnvironment environment, IHTMLUIContext ui) : base(environment)
		{
			this.ui = ui;
		}


		protected async override void Initialize()
		{
			await ui.LoadHTMLPageAsync(new("demo.html"), new());
		}
	}
}

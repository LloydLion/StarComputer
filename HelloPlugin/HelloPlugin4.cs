using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using StarComputer.PluginDevelopmentKit;

namespace HelloPlugin
{
	[Plugin("Hello4")]
	public class HelloPlugin4 : PluginBase
	{
		private readonly IHTMLUIContext ui;


		public HelloPlugin4(IProtocolEnvironment environment, IHTMLUIContext ui) : base(environment)
		{
			this.ui = ui;
		}


		protected async override void Initialize()
		{
			await ui.LoadHTMLPageAsync(new("demo.html"), new());
		}
	}
}

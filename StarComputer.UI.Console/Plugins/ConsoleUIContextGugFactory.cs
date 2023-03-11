using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Loading;
using StarComputer.Common.Abstractions.Plugins.UI.Console;

namespace StarComputer.UI.Console.Plugins
{
	public class ConsoleUIContextGugFactory : IUIContextFactory<IConsoleUIContext>
	{
		private readonly IConsoleUIContext ctx;


		public ConsoleUIContextGugFactory(IConsoleUIContext ctx)
		{
			this.ctx = ctx;
		}


		public IConsoleUIContext CreateContext(PluginLoadingProto plugin)
		{
			return ctx;
		}
	}
}

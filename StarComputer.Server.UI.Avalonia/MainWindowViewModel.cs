using Microsoft.Extensions.DependencyInjection;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.Server.Abstractions;
using StarComputer.UI.Avalonia;
using System;

namespace StarComputer.Server.UI.Avalonia
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IServer server;


		public MainWindowViewModel(IServiceProvider services)
		{
			var browserViewModel = new BrowserViewModel(services.GetRequiredService<HTMLUIManager>(), services.GetRequiredService<IThreadDispatcher<Action>>());
			var connectionViewModel = new ListenViewModel(services.GetRequiredService<IServer>());
			var pluginSelectorViewModel = new PluginSelectorViewModel(services.GetRequiredService<HTMLUIManager>(), services.GetRequiredService<IPluginStore>());

			server = services.GetRequiredService<IServer>();

			Content = new ServerViewModel(browserViewModel, connectionViewModel, pluginSelectorViewModel);
		}


		public ViewModelBase Content { get; private set; }


		public void Close()
		{
			server.Close();
		}
	}
}

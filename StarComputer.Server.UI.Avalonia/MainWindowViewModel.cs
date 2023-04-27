using Microsoft.Extensions.DependencyInjection;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Server.Abstractions;
using StarComputer.UI.Avalonia;
using System;

namespace StarComputer.Server.UI.Avalonia
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IServer server;


		public MainWindowViewModel(MainWindow owner, IServiceProvider services)
		{
			server = services.GetRequiredService<IServer>();

			var browserViewModel = new BrowserViewModel(services.GetRequiredService<IBrowserCollection>());
			var serverControlViewModel = new ServerControlViewModel(server, owner);
			var pluginSelectorViewModel = new PluginSelectorViewModel(browserViewModel, services.GetRequiredService<IPluginStore>());
			var serverStatusBarViewModel = new ServerStatusBarViewModel(server);


			Content = new ServerViewModel(browserViewModel, serverControlViewModel, pluginSelectorViewModel, serverStatusBarViewModel);
		}


		public ViewModelBase Content { get; private set; }


		public async void Close()
		{
			await server.CloseAsync();
		}
	}
}

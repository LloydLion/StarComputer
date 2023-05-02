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

			var browserViewModel = new BrowserViewModel(services.GetRequiredService<IBrowserCollection>(), services.GetRequiredService<IPluginStore>());
			var serverControlViewModel = new ServerControlViewModel(server, owner);
			var serverStatusBarViewModel = new ServerStatusBarViewModel(server);


			Content = new ServerViewModel(browserViewModel, serverControlViewModel, serverStatusBarViewModel);
		}


		public ViewModelBase Content { get; private set; }


		public async void Close()
		{
			await server.CloseAsync();
		}
	}
}

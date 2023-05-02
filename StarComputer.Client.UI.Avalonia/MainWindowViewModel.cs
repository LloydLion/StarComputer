using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.UI.Avalonia;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IClient client;


		public MainWindowViewModel(MainWindow window, IServiceProvider services)
		{
			client = services.GetRequiredService<IClient>();

			var connectionDialogViewModel = new ConnectionDialogViewModel(services.GetRequiredService<IOptions<ConnectionDialogViewModel.Options>>());
			var clientConnectionMenuViewModel = new ClientConnectionMenuViewModel(client, window, connectionDialogViewModel);

			var browserViewModel = new BrowserViewModel(services.GetRequiredService<IBrowserCollection>(), services.GetRequiredService<IPluginStore>());
			var clientStatusBarViewModel = new ClientStatusBarViewModel(client);

			Content = new ClientViewModel(window, browserViewModel, clientConnectionMenuViewModel, clientStatusBarViewModel, client);
		}


		public ViewModelBase Content { get; private set; }


		public async void Close()
		{
			await client.TerminateAsync();
		}
	}
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.UI.Avalonia;
using StarComputer.ApplicationUtils.Localization;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IClient client;


		public MainWindowViewModel(MainWindow window, IServiceProvider services)
		{
			client = services.GetRequiredService<IClient>();

			var localizationFactory = services.GetRequiredService<IStringLocalizerFactory>();

			var connectionDialogViewModel = new ConnectionDialogViewModel(services.GetRequiredService<IOptions<ConnectionDialogViewModel.Options>>(), localizationFactory.Create<ConnectionDialogView>());
			var clientConnectionMenuViewModel = new ClientConnectionMenuViewModel(client, window, connectionDialogViewModel, localizationFactory.Create<ClientConnectionMenuView>());

			var browserViewModel = new BrowserViewModel(services.GetRequiredService<IBrowserCollection>(), services.GetRequiredService<IPluginStore>(), localizationFactory.Create<BrowserView>());
			var clientStatusBarViewModel = new ClientStatusBarViewModel(client, localizationFactory.Create<ClientStatusBarView>());

			ErrorDialogView.LocalizeWith(localizationFactory.Create<ErrorDialogView>());

			Content = new ClientViewModel(window, browserViewModel, clientConnectionMenuViewModel, clientStatusBarViewModel, client);
		}


		public ViewModelBase Content { get; private set; }


		public async void Close()
		{
			await client.TerminateAsync();
		}
	}
}

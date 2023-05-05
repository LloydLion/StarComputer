using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using StarComputer.ApplicationUtils.Localization;
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

			var localizationFactory = services.GetRequiredService<IStringLocalizerFactory>();

			var browserViewModel = new BrowserViewModel(services.GetRequiredService<IBrowserCollection>(), services.GetRequiredService<IPluginStore>(), localizationFactory.Create<BrowserView>());
			var serverControlViewModel = new ServerControlViewModel(server, owner, localizationFactory.Create<ServerControlView>());
			var serverStatusBarViewModel = new ServerStatusBarViewModel(server, localizationFactory.Create<ServerStatusBarView>());

			ErrorDialogView.LocalizeWith(localizationFactory.Create<ErrorDialogView>());

			Content = new ServerViewModel(browserViewModel, serverControlViewModel, serverStatusBarViewModel, localizationFactory.Create<ServerView>());
		}


		public ViewModelBase Content { get; private set; }


		public async void Close()
		{
			await server.CloseAsync();
		}
	}
}

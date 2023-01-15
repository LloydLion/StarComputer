using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.UI.Avalonia;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IClient client;


		public MainWindowViewModel(IServiceProvider services)
		{
			var browserViewModel = new BrowserViewModel(services.GetRequiredService<HTMLUIManager>(), services.GetRequiredService<IThreadDispatcher<Action>>());
			var connectionViewModel = new ConnectionViewModel(services.GetRequiredService<IClient>(), services.GetRequiredService<IOptions<ConnectionViewModel.Options>>());
			var pluginSelectorViewModel = new PluginSelectorViewModel(services.GetRequiredService<HTMLUIManager>(), services.GetRequiredService<IPluginStore>());

			client = services.GetRequiredService<IClient>();

			Content = new ClientViewModel(browserViewModel, connectionViewModel, pluginSelectorViewModel);
		}


		public ViewModelBase Content { get; private set; }


		public void Close()
		{
			client.Close();
		}
	}
}

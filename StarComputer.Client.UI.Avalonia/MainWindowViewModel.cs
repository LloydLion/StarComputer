using Microsoft.Extensions.Options;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.Client.UI.Avalonia
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IClient client;


		public MainWindowViewModel(IClient client, IOptions<ClientViewModel.Options> clientViewOptions, IPluginStore plugins, HTMLUIManager manager)
		{
			var browserViewModel = new BrowserViewModel(manager);
			Content = new ClientViewModel(client, clientViewOptions, manager, plugins, browserViewModel);
			this.client = client;
		}


		public ViewModelBase Content { get; private set; }


		public void Close()
		{
			client.Close();
		}
	}
}

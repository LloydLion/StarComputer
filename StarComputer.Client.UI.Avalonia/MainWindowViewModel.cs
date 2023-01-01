using StarComputer.Client.Abstractions;

namespace StarComputer.Client.UI.Avalonia
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IClient client;


		public MainWindowViewModel(IClient client, AvaloniaBasedConsoleUIContext uiContext)
		{
			Content = new ClientViewModel(client, uiContext);
			this.client = client;
		}


		public ViewModelBase Content { get; private set; }


		public void Close()
		{
			client.GetServerAgent().Disconnect();
		}
	}
}

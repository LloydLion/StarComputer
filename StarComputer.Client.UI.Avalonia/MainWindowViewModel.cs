using StarComputer.Client.Abstractions;

namespace StarComputer.Client.UI.Avalonia
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IClient? client;


		public MainWindowViewModel(IClient? client = null, AvaloniaBasedConsoleUIContext? uIContext = null)
		{
			Content = new ClientViewModel(client, uIContext);
			this.client = client;
		}


		public ViewModelBase Content { get; private set; }


		public void Close()
		{
			client?.GetServerAgent()?.Disconnect();
		}
	}
}

using Avalonia.Threading;

namespace StarComputer.Client.UI.Avalonia
{
	public class BrowserViewModel : ViewModelBase
	{
		private readonly HTMLUIManager manager;


		public BrowserViewModel(HTMLUIManager manager)
		{
			this.manager = manager;
			manager.ActiveContextChanged += OnActivePluginChanged;
		}


		public object? JSContext => manager.ActiveContext?.GetView().JSContext;

		public string? Address => manager.ActiveContext?.GetView().Address;


		private void OnActivePluginChanged(HTMLUIManager.ContextChangingType type)
		{
			if (type == HTMLUIManager.ContextChangingType.ActivePluginChanged || type == HTMLUIManager.ContextChangingType.AddressChanged)
				Dispatcher.UIThread.Post(() => RaisePropertyChanged(nameof(Address)), DispatcherPriority.Send);
			if (type == HTMLUIManager.ContextChangingType.ActivePluginChanged || type == HTMLUIManager.ContextChangingType.JSContextChanged)
				Dispatcher.UIThread.Post(() => RaisePropertyChanged(nameof(JSContext)), DispatcherPriority.Send);
		}
	}
}

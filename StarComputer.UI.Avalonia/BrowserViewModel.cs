using Avalonia.Threading;
using StarComputer.Common.Abstractions.Plugins;
using System;
using System.Collections.Generic;

namespace StarComputer.UI.Avalonia
{
	public class BrowserViewModel : ViewModelBase
	{
		private readonly HTMLUIManager manager;


		public BrowserViewModel(HTMLUIManager manager)
		{
			this.manager = manager;
			manager.ContextChanged += (a, b) => Dispatcher.UIThread.Post(() => ContextChanged?.Invoke(a, b), DispatcherPriority.Send);
		}


		public event Action<HTMLUIManager.ContextChangingType, HTMLUIContext?>? ContextChanged;


		public IReadOnlyDictionary<IPlugin, HTMLUIContext> Contexts => manager.Contexts;


		public void SetJavaScriptExecutor(HTMLUIManager.JavaScriptExecutor executor) => manager.SetJavaScriptExecutor(executor);
	}
}

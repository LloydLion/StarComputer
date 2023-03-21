using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Threading;
using System.Collections;

namespace StarComputer.UI.Avalonia
{
	public class BrowserCollection : IBrowserCollection
	{
		private readonly Dictionary<PluginDomain, PluginAvaloniaBrowser> baseDic = new();
		private readonly IThreadDispatcher<Action> mainThreadDispatcher;
		private readonly LinkedList<EventHandler> onInitializedSubscribers = new();
		private readonly object sychRoot = new();


		public BrowserCollection(IThreadDispatcher<Action> mainThreadDispatcher)
		{
			this.mainThreadDispatcher = mainThreadDispatcher;
		}


		public PluginAvaloniaBrowser this[PluginDomain key]
		{
			get => GetOrCreateElement(key);
		}


		public bool IsBrowserEnvironmentInitialized { get; private set; }


		public IEnumerator<PluginAvaloniaBrowser> GetEnumerator() => baseDic.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public bool IsBrowserCreatedFor(PluginDomain plugin)
		{
			lock (sychRoot)
			{
				return baseDic.ContainsKey(plugin);
			}
		}

		public void OnBrowserEnvironmentInitialized(EventHandler eventHandler)
		{
			lock (sychRoot)
			{
				if (IsBrowserEnvironmentInitialized)
					mainThreadDispatcher.DispatchTask(() => eventHandler.Invoke(this, EventArgs.Empty));
				else onInitializedSubscribers.AddLast(eventHandler);
			}
		}

		private PluginAvaloniaBrowser GetOrCreateElement(PluginDomain plugin)
		{
			lock (sychRoot)
			{
				if (baseDic.TryGetValue(plugin, out var value) == false)
				{
					if (IsBrowserEnvironmentInitialized)
						throw new InvalidOperationException("Enable to create new browser, browser collection already initialized");

					value = new(mainThreadDispatcher, TryCallBrowserEnvironmentInitialized);
					baseDic.Add(plugin, value);
				}

				return value;
			}
		}

		private void TryCallBrowserEnvironmentInitialized()
		{
			lock (sychRoot)
			{
				if (baseDic.Values.All(s => s.IsInitialized))
				{
					IsBrowserEnvironmentInitialized = true;

					foreach (var subscriber in onInitializedSubscribers)
					{
						mainThreadDispatcher.DispatchTask(() => subscriber.Invoke(this, EventArgs.Empty));
					}

					onInitializedSubscribers.Clear();
				}
			}
		}
	}
}

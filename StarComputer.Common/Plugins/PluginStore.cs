using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Loading;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace StarComputer.Common.Plugins
{
	public class PluginStore : IPluginStore
	{
		private readonly Dictionary<string, IPlugin> plugins = new();


		public bool IsInitialized { get; private set; }


		public IPlugin this[string key] => IfInitialized(() => plugins[key]);


		public IEnumerable<string> Keys => IfInitialized(plugins.Keys);

		public IEnumerable<IPlugin> Values => IfInitialized(plugins.Values);

		public int Count => IfInitialized(plugins.Count);


		public void InitializeStore(IPluginLoader loader, IPluginInitializer initializer)
		{
			var data = loader.LoadPlugins();

			var plugins = initializer.InitializePlugins(data);

			foreach (var el in plugins) this.plugins.Add(el.GetDomain(), el);

			IsInitialized = true;
		}

		public bool ContainsKey(string key) => IfInitialized(() => plugins.ContainsKey(key));

		public IEnumerator<KeyValuePair<string, IPlugin>> GetEnumerator() => IfInitialized(plugins.GetEnumerator);

		public bool TryGetValue(string key, [MaybeNullWhen(false)] out IPlugin value)
		{
			IfInitialized();
			return plugins.TryGetValue(key, out value);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


		private T IfInitialized<T>(Func<T> func)
		{
			IfInitialized();
			return func();
		}

		private T IfInitialized<T>(T value)
		{
			IfInitialized();
			return value;
		}

		private void IfInitialized()
		{
			if (IsInitialized == false)
				throw new InvalidOperationException("Initialize store before use");
		}
	}
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Loading;
using StarComputer.Common.Abstractions.Plugins.Resources;

namespace StarComputer.UI.Avalonia
{
	public class HTMLUIManager : IUIContextFactory<HTMLUIContext>
	{
		private readonly Dictionary<PluginDomain, HTMLUIContext> contexts = new();
		private readonly IResourcesCatalog resources;
		private readonly ILogger<HTMLUIManager> logger;
		private readonly Options options;
		private PluginDomain? activePlugin;
		private JavaScriptExecutor? executor;


		public delegate dynamic? JavaScriptExecutor(PluginDomain caller, string functionName, object[] arguments);


		public PluginDomain? ActivePlugin
		{
			get => activePlugin;
			private set
			{
				activePlugin = value;

				if (activePlugin is not null)
					ActiveContext = contexts[activePlugin.Value];
				else ActiveContext = null;

				ContextChanged?.Invoke(ContextChangingType.ActivePluginChanged, ActiveContext);
			}	
		}

		public HTMLUIContext? ActiveContext { get; private set; }

		public IReadOnlyDictionary<PluginDomain, HTMLUIContext> Contexts => contexts;


		public HTMLUIManager(IResourcesCatalog resources, ILogger<HTMLUIManager> logger, IOptions<Options> options)
		{
			this.resources = resources;
			this.logger = logger;
			this.options = options.Value;
		}


		public event Action<ContextChangingType, HTMLUIContext?>? ContextChanged;


		public HTMLUIContext CreateContext(PluginLoadingProto plugin)
		{
			if (contexts.ContainsKey(plugin.Domain)) return contexts[plugin.Domain];
			else
			{
				var context = new HTMLUIContext(this, plugin.Domain, resources.GetResourcesFor(plugin.Domain), logger, options.UniqueHttpPrefix, options.HttpPort);

				context.JSContextChanged += OnJSContextChanged;
				context.NewPageLoaded += OnNewPageLoaded;

				contexts.Add(plugin.Domain, context);

				context.Initialize();

				return context;
			}
		}

		public HTMLUIContext GetContext(PluginDomain plugin) => contexts[plugin];

		public void SwitchPlugin(IPlugin? plugin)
		{
			ActivePlugin = plugin?.GetDomain();
		}

		public void SetJavaScriptExecutor(JavaScriptExecutor executor)
		{
			this.executor = executor;
		}

		public dynamic? ExecuteJavaScript(PluginDomain caller, string functionName, params object[] arguments)
		{
			if (executor is null)
				throw new NullReferenceException("JavaScript executor was null");
			return executor(caller, functionName, arguments);
		}

		private void OnJSContextChanged(object? sender, EventArgs _) =>
			ContextChanged?.Invoke(ContextChangingType.JSContextChanged, (HTMLUIContext?)sender);

		private void OnNewPageLoaded(object? sender, EventArgs _) =>
			ContextChanged?.Invoke(ContextChangingType.AddressChanged, (HTMLUIContext?)sender);

		public void InitializePostUI()
		{
			foreach (var context in contexts)
				context.Value.InitializePostUI();
		}


		public enum ContextChangingType
		{
			ActivePluginChanged,
			AddressChanged,
			JSContextChanged
		}


		public class Options
		{
			public string UniqueHttpPrefix { get; set; } = "starComputer";

			public int HttpPort { get; set; } = 7676;
		}
	}
}

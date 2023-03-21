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
		private readonly IBrowserCollection browsers;
		private readonly IResourcesCatalog resources;
		private readonly ILogger<HTMLUIManager> logger;
		private readonly Options options;
		private bool isPostInitialized;


		public HTMLUIManager(IBrowserCollection browsers, IResourcesCatalog resources, ILogger<HTMLUIManager> logger, IOptions<Options> options)
		{
			this.browsers = browsers;
			this.resources = resources;
			this.logger = logger;
			this.options = options.Value;


			browsers.OnBrowserEnvironmentInitialized((sender, e) =>
			{
				isPostInitialized = true;
				foreach (var item in contexts.Values)
					item.InitializePostUI();
			});
		}


		public HTMLUIContext CreateContext(PluginLoadingProto plugin)
		{
			if (contexts.ContainsKey(plugin.Domain)) return contexts[plugin.Domain];
			else
			{
				if (isPostInitialized)
					throw new InvalidOperationException("Enable to create new HTML PUI context, UI already post initialized");

				var context = new HTMLUIContext(browsers[plugin.Domain], plugin.Domain, resources.GetResourcesFor(plugin.Domain), logger, options.UniqueHttpPrefix, options.HttpPort);

				contexts.Add(plugin.Domain, context);

				context.Initialize();

				return context;
			}
		}


		public class Options
		{
			public string UniqueHttpPrefix { get; set; } = "starComputer";

			public int HttpPort { get; set; } = 7676;
		}
	}
}

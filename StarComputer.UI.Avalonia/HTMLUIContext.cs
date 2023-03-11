using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using System.Text;

namespace StarComputer.UI.Avalonia
{
	public class HTMLUIContext : IHTMLUIContext
	{
		private IHTMLPageConstructor? pageConstructor;
		private readonly HTMLUIManager owner;
		private readonly PluginDomain plugin;
		private readonly IResourcesManager resources;
		private readonly HttpLocalServer server;


		public HTMLUIContext(HTMLUIManager owner, PluginDomain plugin, IResourcesManager resources, ILogger logger, string httpPrefix, int httpPort = 7676)
		{
			this.owner = owner;
			this.plugin = plugin;
			this.resources = resources;

			server = new(resources, logger, Options.Create<HttpLocalServer.Options>(new() { HttpPrefix = string.Concat(httpPrefix, "/", plugin), Port = httpPort }) );
		}


		public object? JSContext { get; private set; }

		public string? Address { get; private set; }

		public PluginDomain Plugin => plugin;


		public event EventHandler? NewPageLoaded;

		public event EventHandler? JSContextChanged;

		public event Action? OnUIPostInitialized;


		public void Initialize()
		{
			server.Start();
		}

		public HTMLPageLoadResult LoadEmptyPage()
		{
			Address = null;
			NewPageLoaded?.Invoke(this, EventArgs.Empty);

			return new();
		}

		public HTMLPageLoadResult LoadHTMLPage(PluginResource resource, PageConstructionBag constructionBag)
		{
			string document;
			if (pageConstructor is null)
			{
				using var reader = new StreamReader(resources.ReadResource(resource));
				var documentBuilder = new StringBuilder(reader.ReadToEnd());

				foreach (var argument in constructionBag.ConstructionArguments)
					documentBuilder.Replace($"{{{{{argument.Key}}}}}", argument.Value);

				document = documentBuilder.ToString();
			}
			else document = pageConstructor.ConstructHTMLPage(resource, constructionBag);

			server.ReplaceFile(new PluginResource("index.html"), document, "text/html");

			Address = server.HttpPrefix + "index.html";
			NewPageLoaded?.Invoke(this, EventArgs.Empty);

			return new();
		}

		public void SetJSPluginContext(object contextObject)
		{
			JSContext = contextObject;
			JSContextChanged?.Invoke(this, EventArgs.Empty);
		}

		public void UseHTMLPageConstructor(IHTMLPageConstructor? pageConstructor)
		{
			this.pageConstructor = pageConstructor;
		}

		public dynamic? ExecuteJavaScriptFunction(string functionName, params object[] arguments)
		{
			return owner.ExecuteJavaScript(plugin, functionName, arguments);
		}

		public void InitializePostUI()
		{
			OnUIPostInitialized?.Invoke();
		}
	}
}

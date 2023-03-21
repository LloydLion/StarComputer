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
		private readonly PluginAvaloniaBrowser browser;
		private readonly IResourcesManager resources;
		private readonly HttpLocalServer server;
		private EventHandler? uiPostInitHandler;


		public HTMLUIContext
		(
			PluginAvaloniaBrowser browser,
			PluginDomain plugin,
			IResourcesManager resources,
			ILogger logger,
			string httpPrefix,
			int httpPort = 7676
		)
		{
			this.browser = browser;
			this.resources = resources;

			server = new(resources, logger, Options.Create<HttpLocalServer.Options>(new() { HttpPrefix = string.Concat(httpPrefix, "/", plugin), Port = httpPort }) );
		}


		public void Initialize()
		{
			server.Start();
			LoadEmptyPage();
		}

		public HTMLPageLoadResult LoadEmptyPage()
		{
			browser.Navigate("gugpage.html", forceReload: true);

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

			var address = server.HttpPrefix + "index.html";

			browser.Navigate(address, forceReload: true);

			return new();
		}

		public void SetJSPluginContext(object contextObject)
		{
			browser.SetJavaScriptContext(contextObject);
		}

		public void UseHTMLPageConstructor(IHTMLPageConstructor? pageConstructor)
		{
			this.pageConstructor = pageConstructor;
		}

		public dynamic? ExecuteJavaScriptFunction(string functionName, params object[] arguments)
		{
			return browser.ExecuteJavaScript(functionName, arguments);
		}

		public void OnUIPostInitialized(EventHandler handler)
		{
			uiPostInitHandler = handler;
		}

		internal void InitializePostUI()
		{
			uiPostInitHandler?.Invoke(this, EventArgs.Empty);
		}
	}
}

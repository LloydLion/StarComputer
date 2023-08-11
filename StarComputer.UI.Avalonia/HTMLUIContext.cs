using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

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
			server.AddGugpage(new("gugpage.html"));
			LoadEmptyPageAsync().AsTask().Equals(null);
		}

		public async ValueTask<HTMLPageLoadResult> LoadEmptyPageAsync()
		{
			await browser.NavigateAsync("gugpage.html", forceReload: true);

			return new();
		}

		public async ValueTask<HTMLPageLoadResult> LoadHTMLPageAsync(PluginResource resource, PageConstructionBag constructionBag)
		{
			string document;
			if (pageConstructor is null)
			{
				using var reader = new StreamReader(resources.ReadResource(resource));
				var raw = await reader.ReadToEndAsync();
				var documentBuilder = new StringBuilder(raw);

				if (constructionBag.Localizer is not null)
				{
					var localizationMatches = Regex.Matches(raw, "{{\\$(\\w*)}}");
					foreach (var match in localizationMatches.AsEnumerable())
					{
						var key = match.Groups[1];
						var value = constructionBag.Localizer[key.Value];
						documentBuilder.Replace(match.Value, value);
					}
				}

				foreach (var argument in constructionBag.ConstructionArguments)
					documentBuilder.Replace($"{{{{{argument.Key}}}}}", argument.Value);

				document = documentBuilder.ToString();
			}
			else document = pageConstructor.ConstructHTMLPage(resource, constructionBag);

			const string indexAddress = "index.html";
			server.CancelFileReplacement(new PluginResource(indexAddress));
			server.ReplaceFile(new PluginResource(indexAddress), document, "text/html");

			var address = server.HttpPrefix + indexAddress;

			await browser.NavigateAsync(address, forceReload: true);

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

		public dynamic? ExecuteJavaScriptFunction(string functionName, params object?[] arguments)
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

		public string ShareResource(PluginResource resource, ReadOnlyMemory<byte> fileData, string contentType, string? charset = null)
		{
			server.ReplaceFile(resource, fileData, contentType, charset);
			return server.HttpPrefix + resource.FullPath;
		}

		public void StopResourceShare(PluginResource resource)
		{
			server.CancelFileReplacement(resource);
		}
	}
}

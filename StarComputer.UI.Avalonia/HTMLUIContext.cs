using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StarComputer.UI.Avalonia
{
	public class HTMLUIContext : IHTMLUIContext
	{
		private IHTMLPageConstructor? pageConstructor;
		private readonly HTMLUIManager owner;
		private readonly IPlugin plugin;
		private readonly IResourcesManager resources;


		public HTMLUIContext(HTMLUIManager owner, IPlugin plugin, IResourcesManager resources)
		{
			this.owner = owner;
			this.plugin = plugin;
			this.resources = resources;
		}


		public object? JSContext { get; private set; }

		public string? Address { get; private set; }

		public IPlugin Plugin => plugin;


		public event EventHandler? NewPageLoaded;

		public event EventHandler? JSContextChanged;

		public event Action? OnUIPostInitialized;


		public HTMLPageLoadResult LoadEmptyPage()
		{
			Address = null;
			NewPageLoaded?.Invoke(this, EventArgs.Empty);

			return new();
		}

		public HTMLPageLoadResult LoadHTMLPage(string resourceName, PageConstructionBag constructionBag)
		{
			string document;
			if (pageConstructor is null)
			{
				using var reader = new StreamReader(resources.OpenRead(resourceName));
				var documentBuilder = new StringBuilder(reader.ReadToEnd());

				foreach (var argument in constructionBag.ConstructionArguments)
					documentBuilder.Replace($"{{{{{argument.Key}}}}}", argument.Value);

				document = documentBuilder.ToString();
			}
			else document = pageConstructor.ConstructHTMLPage(resourceName, constructionBag);

			using (var temporalFile = resources.OpenTemporalFile("HTML"))
			{
				var writer = new StreamWriter(temporalFile) { AutoFlush = true };
				writer.Write(document);
				Address = FilePathToFileUrl(temporalFile.Name);
			}

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

		private static string FilePathToFileUrl(string filePath)
		{
			var uri = new StringBuilder();
			foreach (char v in filePath)
			{
				if ((v >= 'a' && v <= 'z') || (v >= 'A' && v <= 'Z') || (v >= '0' && v <= '9') ||
				  v == '+' || v == '/' || v == ':' || v == '.' || v == '-' || v == '_' || v == '~' ||
				  v > '\xFF') uri.Append(v);
				else if (v == Path.DirectorySeparatorChar || v == Path.AltDirectorySeparatorChar)
					uri.Append('/');
				else uri.Append(string.Format("%{0:X2}", (int)v));
			}

			if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') // UNC path
				uri.Insert(0, "file:");
			else
				uri.Insert(0, "file:///");

			return uri.ToString();
		}

		public void InitializePostUI()
		{
			OnUIPostInitialized?.Invoke();
		}
	}
}

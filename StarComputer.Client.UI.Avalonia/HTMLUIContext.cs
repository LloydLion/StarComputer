using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StarComputer.Client.UI.Avalonia
{
	public class HTMLUIContext : IHTMLUIContext
	{
		private IHTMLPageConstructor? pageConstructor;
		private readonly HTMLUIManager owner;
		private readonly IPlugin plugin;
		private readonly HTMLView view = new();


		public HTMLUIContext(HTMLUIManager owner, IPlugin plugin)
		{
			this.owner = owner;
			this.plugin = plugin;
		}


		public async ValueTask<HTMLPageLoadResult> LoadHTMLPageAsync(string resourceName, PageConstructionBag constructionBag)
		{
			var directory = Path.Combine("../../../../resources", plugin.Domain);

			string document;
			if (pageConstructor is null)
				document = await File.ReadAllTextAsync(Path.Combine(directory, resourceName));
			else document = pageConstructor.ConstructHTMLPage(resourceName, constructionBag);

			var newDocumentPath = Path.Combine(directory, "starcomputer.temporal.html");
			await File.WriteAllTextAsync(newDocumentPath, document);

			view.Address = FilePathToFileUrl(Path.GetFullPath(newDocumentPath));

			return new();
		}

		public void SetJSPluginContext(object contextObject)
		{
			view.JSContext = contextObject;
		}

		public void UseHTMLPageConstructor(IHTMLPageConstructor? pageConstructor)
		{
			this.pageConstructor = pageConstructor;
		}

		public HTMLView GetView() => view;

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


		public class HTMLView : INotifyPropertyChanged
		{
			private object? jSContext;
			private string? address;


			public event PropertyChangedEventHandler? PropertyChanged;


			public object? JSContext { get => jSContext; set { jSContext = value; PropertyChanged?.Invoke(this, new(nameof(JSContext))); } }

			public string? Address { get => address; set { address = value; PropertyChanged?.Invoke(this, new(nameof(Address))); } }
		}
	}
}

using Avalonia.Controls;
using StarComputer.Common.Abstractions.Plugins;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using Xilium.CefGlue.Avalonia;

namespace StarComputer.UI.Avalonia
{
	public partial class BrowserView : UserControl
	{
		public const string JSContextFieldName = "context";


		private readonly Dictionary<IPlugin, AvaloniaCefBrowser> browsers = new();


		private BrowserViewModel Context => (BrowserViewModel)DataContext!;


		public BrowserView()
		{
			InitializeComponent();

			if (Design.IsDesignMode == false)
			{
				Initialized += OnBrowserViewInitialized;
				Background = null;
			}
		}


		private void OnBrowserViewInitialized(object? sender, EventArgs e)
		{
			Context.ContextChanged += (type, ctx) =>
			{
				if (type == HTMLUIManager.ContextChangingType.ActivePluginChanged)
				{
					browserFrame.Child = GetBrowser(ctx?.Plugin);
				}
				else if (type == HTMLUIManager.ContextChangingType.AddressChanged)
				{
					if (ctx is not null)
					{
						GetBrowser(ctx.Plugin).Address = ctx?.Address ?? "file:///gugpage.html";
					}
				}
				else //HTMLUIManager.ContextChangingType.JSContextChanged
				{
					if (ctx is not null)
					{
						var browser = GetBrowser(ctx.Plugin);

						if (ctx.JSContext is not null)
							browser.RegisterJavascriptObject(ctx.JSContext, JSContextFieldName);
						else browser.UnregisterJavascriptObject(JSContextFieldName);
					}
				}
			};


			foreach (var ctx in Context.Contexts)
			{
				var browser = GetBrowser(ctx.Key);

				browser.Address = ctx.Value.Address ?? "file:///gugpage.html";

				if (ctx.Value.JSContext is not null)
					browser.RegisterJavascriptObject(ctx.Value.JSContext, JSContextFieldName);
			}


			Context.SetJavaScriptExecutor(ExecuteJavaScript);
		}

		private dynamic? ExecuteJavaScript(IPlugin caller, string functionName, string[] arguments)
		{
			var code = $"{functionName}({string.Join(", ", arguments)});";
			return GetBrowser(caller).EvaluateJavaScript<ExpandoObject>(code).Result;
		}

		[return: NotNullIfNotNull("plugin")]
		private AvaloniaCefBrowser? GetBrowser(IPlugin? plugin)
		{
			if (plugin is null) return null;
			else if (browsers.TryGetValue(plugin, out var val)) return val;
			else
			{
				var browser = new AvaloniaCefBrowser();
				browsers.Add(plugin, browser);
				return browser;
			}
		}
	}
}

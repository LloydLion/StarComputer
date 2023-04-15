using StarComputer.Common.Abstractions.Plugins.Resources;

namespace StarComputer.Common.Abstractions.Plugins.UI.HTML
{
	public interface IHTMLUIContext : IUIContext
	{
		public ValueTask<HTMLPageLoadResult> LoadEmptyPageAsync();

		public ValueTask<HTMLPageLoadResult> LoadHTMLPageAsync(PluginResource resource, PageConstructionBag constructionBag);

		public dynamic? ExecuteJavaScriptFunction(string functionName, params object?[] arguments);

		public void UseHTMLPageConstructor(IHTMLPageConstructor? pageConstructor);

		public void SetJSPluginContext(object contextObject);

		public void OnUIPostInitialized(EventHandler handler);

		public string ShareResource(PluginResource resource, ReadOnlyMemory<byte> fileData, string contentType, string? charset = null);

		public void StopResourceShare(PluginResource resource);
	}
}

namespace StarComputer.Common.Abstractions.Plugins.UI.HTML
{
	public interface IHTMLUIContext : IUIContext
	{
		public HTMLPageLoadResult LoadHTMLPage(string resourceName, PageConstructionBag constructionBag);

		public dynamic? ExecuteJavaScriptFunction(string functionName, params string[] arguments);

		public void UseHTMLPageConstructor(IHTMLPageConstructor? pageConstructor);

		public void SetJSPluginContext(object contextObject);
	}
}

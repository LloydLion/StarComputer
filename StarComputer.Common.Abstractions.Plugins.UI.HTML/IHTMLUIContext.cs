namespace StarComputer.Common.Abstractions.Plugins.UI.HTML
{
	public interface IHTMLUIContext : IUIContext
	{
		public ValueTask<HTMLPageLoadResult> LoadHTMLPageAsync(string resourceName, PageConstructionBag constructionBag);

		public void UseHTMLPageConstructor(IHTMLPageConstructor? pageConstructor);

		public void SetJSPluginContext(object contextObject);
	}
}

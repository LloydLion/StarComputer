namespace StarComputer.Common.Abstractions.Plugins.HTML
{
	public interface IHTMLUIContext
	{
		public ValueTask<HTMLPageLoadResult> LoadHTMLPageAsync(string resourceName, PageConstructionBag constructionBag);

		public void UseHTMLPageConstructor(IHTMLPageConstructor? pageConstructor);

		public void SetJSPluginContext(object contextObject);

		public void StartJSExecution();
	}
}

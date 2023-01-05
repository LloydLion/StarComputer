namespace StarComputer.Common.Abstractions.Plugins.UI.HTML
{
	public interface IHTMLPageConstructor
	{
		public string ConstructHTMLPage(string resourceName, PageConstructionBag constructionBag);
	}
}
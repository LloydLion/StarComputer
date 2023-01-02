namespace StarComputer.Common.Abstractions.Plugins.UI.HTML
{
	public interface IHTMLPageConstructor
	{
		public ReadOnlySpan<char> ConstructHTMLPage(string resourceName, PageConstructionBag constructionBag);
	}
}
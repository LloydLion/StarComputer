namespace StarComputer.Common.Abstractions.Plugins.HTML
{
	public interface IHTMLPageConstructor
	{
		public ReadOnlySpan<char> ConstructHTMLPage(string resourceName, PageConstructionBag constructionBag);
	}
}
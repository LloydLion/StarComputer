using StarComputer.Common.Abstractions.Plugins.Resources;

namespace StarComputer.Common.Abstractions.Plugins.UI.HTML
{
	public interface IHTMLPageConstructor
	{
		public string ConstructHTMLPage(PluginResource resource, PageConstructionBag constructionBag);
	}
}
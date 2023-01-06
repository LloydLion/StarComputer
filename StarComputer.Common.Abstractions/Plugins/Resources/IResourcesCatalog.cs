namespace StarComputer.Common.Abstractions.Plugins.Resources
{
	public interface IResourcesCatalog
	{
		public IResourcesManager GetResourcesFor(IPlugin plugin);
	}
}

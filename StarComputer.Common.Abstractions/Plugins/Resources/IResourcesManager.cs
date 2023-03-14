namespace StarComputer.Common.Abstractions.Plugins.Resources
{
	public interface IResourcesManager
	{
		public PluginDomain TargetPlugin { get; }


		public IEnumerable<PluginResource> ListResources();

		public Stream ReadResource(PluginResource resource);

		public bool HasResource(PluginResource resource);
	}
}

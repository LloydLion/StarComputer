using Microsoft.Extensions.Options;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;

namespace StarComputer.Common.Plugins.Resources
{
	public class ResourcesCatalog : IResourcesCatalog
	{
		private readonly Options options;
		private readonly Dictionary<IPlugin, IResourcesManager> managers = new();


		public ResourcesCatalog(IOptions<Options> options)
		{
			this.options = options.Value;
		}


		public IResourcesManager GetResourcesFor(IPlugin plugin)
		{
			if (managers.ContainsKey(plugin)) return managers[plugin];
			else
			{
				var manager = new ResourcesManager(Path.Combine(options.Path, plugin.Domain), plugin);
				managers.Add(plugin, manager);
				return manager;
			}
		}


		public class Options
		{
			public string Path { get; set; } = "resources";
		}
	}
}

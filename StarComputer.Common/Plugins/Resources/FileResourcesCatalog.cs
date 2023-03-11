using Microsoft.Extensions.Options;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;

namespace StarComputer.Common.Plugins.Resources
{
	public class FileResourcesCatalog : IResourcesCatalog
	{
		private readonly Options options;
		private readonly Dictionary<PluginDomain, IResourcesManager> managers = new();


		public FileResourcesCatalog(IOptions<Options> options)
		{
			this.options = options.Value;
		}


		public IResourcesManager GetResourcesFor(PluginDomain plugin)
		{
			if (managers.ContainsKey(plugin)) return managers[plugin];
			else
			{
				var manager = new FileResourcesManager(options.Path, plugin);
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

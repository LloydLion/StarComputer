using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;

namespace StarComputer.Common.Plugins.Resources
{
	internal class ResourcesManager : IResourcesManager
	{
		private readonly string path;


		public IPlugin TargetPlugin { get; }


		public ResourcesManager(string path, IPlugin plugin)
		{
			this.path = path;
			TargetPlugin = plugin;
		}


		public FileStream OpenRead(string resourceName) => File.OpenRead(Path.Combine(path, resourceName));

		public FileStream OpenTemporalFile(string extension) => File.Open(Path.Combine(path, "starcomputer.temporal." + extension), FileMode.Create);
	}
}

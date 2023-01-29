using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;

namespace StarComputer.Common.Plugins.Resources
{
	internal class ResourcesManager : IResourcesManager
	{
		private readonly string path;
		private readonly string temporalFileName;


		public IPlugin TargetPlugin { get; }


		public ResourcesManager(string path, IPlugin plugin, string temporalFileName)
		{
			this.path = path;
			TargetPlugin = plugin;
			this.temporalFileName = temporalFileName;
		}


		public FileStream OpenRead(string resourceName) => File.OpenRead(Path.Combine(path, resourceName));

		public FileStream OpenTemporalFile(string extension) => File.Open(Path.Combine(path, temporalFileName + "." + extension), FileMode.Create);
	}
}

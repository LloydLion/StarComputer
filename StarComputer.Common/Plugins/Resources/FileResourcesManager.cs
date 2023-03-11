using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;

namespace StarComputer.Common.Plugins.Resources
{
	internal class FileResourcesManager : IResourcesManager
	{
		private readonly string root;


		public PluginDomain TargetPlugin { get; }


		public FileResourcesManager(string path, PluginDomain plugin)
		{
			root = Path.Combine(path, plugin);
			TargetPlugin = plugin;
		}

		public IEnumerable<PluginResource> ListResources()
		{
			return ListResources(root);
		}

		private IEnumerable<PluginResource> ListResources(string targetPath)
		{
			foreach (var item in Directory.EnumerateFiles(targetPath))
				yield return new PluginResource(Path.GetRelativePath(root, item));

			foreach (var item in Directory.EnumerateDirectories(targetPath))
				foreach (var file in ListResources(item))
					yield return file;
		}

		public Stream ReadResource(PluginResource resource)
		{
			return File.OpenRead(Path.Combine(root, resource.FullPath));
		}
	}
}

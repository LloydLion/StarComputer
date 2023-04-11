using Microsoft.Extensions.Options;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Persistence;

namespace StarComputer.Common.Plugins.Persistence
{
	public class FileBasedPluginPersistenceServiceProvider : IPluginPersistenceServiceProvider
	{
		private readonly Options options;


		public FileBasedPluginPersistenceServiceProvider(IOptions<Options> options)
		{
			this.options = options.Value;
		}


		public IPluginPersistenceService Provide(PluginDomain domain)
		{
			return new FileBasedPluginPersistenceService(Path.Combine(options.BasePath, domain.Domain));
		}


		public class Options
		{
			public string BasePath { get; set; } = "persistence";
		}
	}
}

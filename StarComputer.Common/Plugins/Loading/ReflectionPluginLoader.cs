using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Loading;
using System.Reflection;

namespace StarComputer.Common.Plugins.Loading
{
	public class ReflectionPluginLoader : IPluginLoader
	{
		private static readonly EventId LoadingDirectoryID = new(11, "LoadingDirectory");
		private static readonly EventId PluginLoadedID = new(12, "PluginLoaded");
		private static readonly EventId LoadingDllID = new(13, "LoadingDll");
		private static readonly EventId PluginCreationErrorID = new(21, "PluginCreationError");
		private static readonly EventId DllLoadErrorID = new(22, "DllLoadError");
		private static readonly EventId DirectoryNotFoundID = new(23, "DirectoryNotFound");


		private readonly Options options;
		private readonly ILogger<ReflectionPluginLoader> logger;


		public ReflectionPluginLoader(IOptions<Options> options, ILogger<ReflectionPluginLoader> logger)
		{
			this.options = options.Value;
			this.logger = logger;
		}


		public ValueTask<IEnumerable<IPlugin>> LoadPluginsAsync()
		{
			var plugins = new List<IPlugin>();

			foreach (var directory in options.GetPluginDirectories())
			{
				logger.Log(LogLevel.Trace, LoadingDirectoryID, "Finding for plugins in {Directory}", directory);

				if (Directory.Exists(directory) == false)
				{
					logger.Log(LogLevel.Warning, DirectoryNotFoundID, "Directory {Directory} is not exits, but in load list", directory);
					continue;
				}

				var dlls = Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories);

				foreach (var dll in dlls)
				{
					logger.Log(LogLevel.Trace, LoadingDllID, "Trying to load DLL file at {FileLocation}", dll);

					try
					{
						var path = Path.GetFullPath(dll);
						var assembly = Assembly.LoadFile(path);
						var pluginTypes = assembly.GetTypes().Where(s => s.IsAssignableTo(typeof(IPlugin)) && s.GetCustomAttribute<PluginAttribute>());

						foreach (var pluginType in pluginTypes)
						{
							try
							{
								var plugin = (IPlugin)(Activator.CreateInstance(pluginType) ?? throw new NullReferenceException());
								plugins.Add(plugin);
								logger.Log(LogLevel.Information, PluginLoadedID, "Plugin ({Domain}[{PluginType}]) loaded successfully from {FileLocation}",
									plugin.Domain, pluginType, dll);
							}
							catch (Exception)
							{
								logger.Log(LogLevel.Error, PluginCreationErrorID, "Enable to create instance of {PluginType}", pluginType);
							}
						}
					}
					catch (Exception)
					{
						logger.Log(LogLevel.Debug, DllLoadErrorID, "Enable to load DLL file at {FileLocation}", dll);
					}
				}
			}

			return ValueTask.FromResult<IEnumerable<IPlugin>>(plugins);
		}


		public class Options
		{
			public const char DirectoriesSeporatorChar = ';';


			public string? PluginDirectories { get; set; }


			public string[] GetPluginDirectories()
			{
				return PluginDirectories?.Split(DirectoriesSeporatorChar) ?? Array.Empty<string>();
			}
		}
	}
}

using Microsoft.Extensions.Options;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Loading;
using System.Reflection;

namespace StarComputer.Common.Plugins.Loading
{
	public class ReflectionPluginLoader : IPluginLoader
	{
		private readonly Options options;


		public ReflectionPluginLoader(IOptions<Options> options)
		{
			this.options = options.Value;
		}


		public ValueTask<IEnumerable<IPlugin>> LoadPluginsAsync()
		{
			var plugins = new List<IPlugin>();

			foreach (var directory in options.GetPluginDirectories())
			{
				if (Directory.Exists(directory) == false)
					continue;

				var dlls = Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories);

				foreach (var dll in dlls)
				{
					try
					{
						var path = Path.GetFullPath(dll);
						var assembly = Assembly.LoadFile(path);
						var pluginTypes = assembly.GetTypes().Where(s => s.IsAssignableTo(typeof(IPlugin)));

						foreach (var pluginType in pluginTypes)
						{
							try
							{
								var plugin = (IPlugin)(Activator.CreateInstance(pluginType) ?? throw new NullReferenceException());
								plugins.Add(plugin);
							}
							catch (Exception)
							{

							}
						}
					}
					catch (Exception)
					{

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

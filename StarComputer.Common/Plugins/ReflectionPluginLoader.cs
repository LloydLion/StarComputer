using Microsoft.Extensions.Options;
using StarComputer.Common.Abstractions.Plugins;
using System.Reflection;

namespace StarComputer.Common.Plugins
{
	public class ReflectionPluginLoader : IPluginLoader
	{
		private readonly Options options;


		public ReflectionPluginLoader(IOptions<Options> options)
		{
			this.options = options.Value;
		}


		public async ValueTask<IEnumerable<IPlugin>> LoadPluginsAsync()
		{
			var plugins = new List<IPlugin>();

			foreach (var directory in options.PluginDirectories)
			{
				if (Directory.Exists(directory) == false)
					continue;

				var dlls = Directory.EnumerateFiles(directory, "*.dll");

				foreach (var dll in dlls)
				{
					try
					{
						var asmBytes = await File.ReadAllBytesAsync(dll);
						var assembly = Assembly.Load(asmBytes);
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

			return plugins;
		}


		public class Options
		{
			private string[]? pluginDirectories;


			public string[] PluginDirectories { get => pluginDirectories ??= new[] { "plugins" }; set => pluginDirectories = value; }
		}
	}
}

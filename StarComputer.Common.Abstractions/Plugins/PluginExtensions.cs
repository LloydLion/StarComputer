using System.Reflection;

namespace StarComputer.Common.Abstractions.Plugins
{
	public static class PluginExtensions
	{
		private readonly static Dictionary<IPlugin, PluginDomain> globalCache = new();


		public static PluginDomain GetDomain(this IPlugin plugin)
		{
			if (globalCache.TryGetValue(plugin, out var result)) return result;

			var ptype = plugin.GetType();
			var domain = (ptype.GetCustomAttribute<PluginAttribute>() ?? throw new NullReferenceException("No PluginAttribute was found on " + ptype.FullName)).Domain;
			globalCache.Add(plugin, domain);
			return domain;
		}
	}
}

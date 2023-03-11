using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Loading;
using StarComputer.Common.Abstractions.Protocol.Bodies;

namespace StarComputer.Common.Plugins
{
	public class PluginInitializer : IPluginInitializer
	{
		private readonly IBodyTypeResolverBuilder resolverBuilder;
		private Action<PluginServiceProvider, PluginLoadingProto>? serviceCreator;


		public PluginInitializer(IBodyTypeResolverBuilder resolverBuilder)
		{
			this.resolverBuilder = resolverBuilder;
		}


		public void SetServices(Action<PluginServiceProvider, PluginLoadingProto> serviceCreator)
		{
			this.serviceCreator = serviceCreator;
		}


		public IEnumerable<IPlugin> InitializePlugins(IEnumerable<PluginLoadingProto> plugins)
		{
			foreach (var plugin in plugins)
			{
				var services = new PluginServiceProvider();

				serviceCreator?.Invoke(services, plugin);

				var instance = plugin.InstantiatePlugin(services);

				resolverBuilder.SetupDomain(instance.GetDomain());

				instance.Initialize(resolverBuilder);

				yield return instance;
			}

			resolverBuilder.ResetDomain();
		}


		public class PluginServiceProvider : IServiceProvider
		{
			private readonly Dictionary<Type, object> services = new();


			public object? GetService(Type serviceType)
			{
				services.TryGetValue(serviceType, out object? result);
				return result;
			}

			public void Register<TService>(TService service) where TService : class
			{
				services.Add(typeof(TService), service);
			}
		}
	}
}

using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Loading;
using StarComputer.Common.Abstractions.Protocol.Bodies;

namespace StarComputer.Common.Plugins
{
	public class PluginInitializer : IPluginInitializer
	{
		private readonly IBodyTypeResolverBuilder resolverBuilder;
		private readonly SynchronizationContext targetSynchronizationContext;
		private Action<PluginServiceProvider, PluginLoadingProto>? serviceCreator;


		public PluginInitializer(IBodyTypeResolverBuilder resolverBuilder, SynchronizationContext targetSynchronizationContext)
		{
			this.resolverBuilder = resolverBuilder;
			this.targetSynchronizationContext = targetSynchronizationContext;
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

				var prevContext = SynchronizationContext.Current;
				SynchronizationContext.SetSynchronizationContext(targetSynchronizationContext);
				instance.Initialize(resolverBuilder);
				SynchronizationContext.SetSynchronizationContext(prevContext);

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

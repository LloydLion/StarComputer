namespace StarComputer.Common.Abstractions.Plugins.Loading
{
	public delegate IPlugin PluginInstantiator(IServiceProvider services);


	public record struct PluginLoadingProto(PluginDomain Domain, PluginInstantiator Instantiator)
	{
		public IPlugin InstantiatePlugin(IServiceProvider services) => Instantiator(services);
	}
}

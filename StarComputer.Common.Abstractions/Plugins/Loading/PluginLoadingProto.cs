﻿namespace StarComputer.Common.Abstractions.Plugins.Loading
{
	public delegate IPlugin PluginInstantiator(IServiceProvider services);


	public record struct PluginLoadingProto(PluginDomain Domain, Type PluginType, PluginInstantiator Instantiator)
	{
		public IPlugin InstantiatePlugin(IServiceProvider services) => Instantiator(services);
	}
}

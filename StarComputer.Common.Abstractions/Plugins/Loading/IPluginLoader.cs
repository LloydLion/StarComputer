﻿namespace StarComputer.Common.Abstractions.Plugins.Loading
{
	public interface IPluginLoader
	{
		public ValueTask<IEnumerable<PluginLoadingProto>> LoadPluginsAsync();
	}
}

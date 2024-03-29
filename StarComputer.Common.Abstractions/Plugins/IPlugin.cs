﻿using StarComputer.Common.Abstractions.Plugins.Protocol;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Protocol.Bodies;

namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IPlugin
	{
		public Version Version { get; }


		public void Initialize(IBodyTypeResolverBuilder resolverBuilder);

		public ValueTask ProcessMessageAsync(PluginProtocolMessage message, MessageContext messageContext);
	}
}

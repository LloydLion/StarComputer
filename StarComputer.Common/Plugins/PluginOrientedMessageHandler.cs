using Microsoft.Extensions.Logging;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Common.Plugins
{
	public class PluginOrientedMessageHandler : IMessageHandler
	{
		private static readonly EventId MessageHandledID = new(41, "MessageHandled");
		private static readonly EventId UnknownMessageDomainID = new(42, "UnknownMessageDomain");


		private readonly IPluginStore plugins;
		private readonly ILogger<PluginOrientedMessageHandler> logger;


		public PluginOrientedMessageHandler(IPluginStore plugins, ILogger<PluginOrientedMessageHandler> logger)
		{
			this.plugins = plugins;
			this.logger = logger;
		}


		public async Task HandleMessageAsync(ProtocolMessage message, IRemoteProtocolAgent agent)
		{
			if (plugins.TryGetValue(message.Domain, out var plugin))
			{
				logger.Log(LogLevel.Debug, MessageHandledID, "Message handled by {PluginType} (domain: {Domain})", plugin.GetType().FullName, plugin.Domain);

				await plugin.ProcessMessageAsync(message, new Context(agent));
			}
			else
			{
				logger.Log(LogLevel.Warning, UnknownMessageDomainID, "Unknown message domain - {Message}", message);
			}
		}

		private class Context : IMessageContext
		{
			public IRemoteProtocolAgent Agent { get; }


			public Context(IRemoteProtocolAgent agent)
			{
				Agent = agent;
			}
		}
	}
}

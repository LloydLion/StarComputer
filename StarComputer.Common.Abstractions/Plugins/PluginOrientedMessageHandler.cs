using Microsoft.Extensions.Logging;
using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Common.Abstractions.Plugins
{
	public class PluginOrientedMessageHandler : IMessageHandler
	{
		private static readonly EventId MessageHandledID = new(41, "MessageHandled");
		private static readonly EventId UnknownMessageDomainID = new(42, "UnknownMessageDomain");


		private readonly IEnumerable<IPlugin> plugins;
		private readonly ILogger<PluginOrientedMessageHandler> logger;


		public PluginOrientedMessageHandler(IEnumerable<IPlugin> plugins, ILogger<PluginOrientedMessageHandler> logger)
		{
			this.plugins = plugins;
			this.logger = logger;
		}


		public async Task HandleMessageAsync(ProtocolMessage message, IRemoteProtocolAgent agent)
		{
			foreach (var plugin in plugins)
			{
				if (plugin.Domain == message.Domain)
				{
					logger.Log(LogLevel.Debug, MessageHandledID, "Message handled by {PluginType} (domain: {Domain})", plugin.GetType().FullName, plugin.Domain);

					await plugin.ProcessMessageAsync(message, new Context(agent));

					return;
				}
			}

			logger.Log(LogLevel.Warning, UnknownMessageDomainID, "Unknown message domain - {Message}", message);
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

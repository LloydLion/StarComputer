using Microsoft.Extensions.Logging;
using StarComputer.Shared.Protocol;

namespace StarComputer.Shared.Plugins
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


		public async Task HandleMessageAsync(ProtocolMessage message, RemoteProtocolAgent agent)
		{
			foreach (var plugin in plugins)
			{
				if (plugin.Domain == message.Domain)
				{
					logger.Log(LogLevel.Debug, MessageHandledID, "Message handled by {PluginType} (domain: {Domain})", plugin.GetType().FullName, plugin.Domain);

					await plugin.HandleMessageAsync(message, new Context(agent));

					return;
				}
			}

			logger.Log(LogLevel.Warning, UnknownMessageDomainID, "Unknown message domain - {Message}", message);
		}

		private class Context : IMessageContext
		{
			public RemoteProtocolAgent Agent { get; }


			public Context(RemoteProtocolAgent agent)
			{
				Agent = agent;
			}
		}
	}
}

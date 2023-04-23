using Microsoft.Extensions.Logging;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Protocol;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Plugins.Protocol;

namespace StarComputer.Common.Plugins
{
	public class PluginOrientedMessageHandler : IMessageHandler
	{
		private static readonly EventId MessageHandledID = new(41, "MessageHandled");
		private static readonly EventId UnknownMessageDomainID = new(42, "UnknownMessageDomain");


		private readonly IPluginStore plugins;
		private readonly ILogger<PluginOrientedMessageHandler> logger;
		private readonly Dictionary<(IRemoteProtocolAgent, IPlugin), IPluginRemoteAgent> agentsCache = new();


		public PluginOrientedMessageHandler(IPluginStore plugins, ILogger<PluginOrientedMessageHandler> logger)
		{
			this.plugins = plugins;
			this.logger = logger;
		}


		public async Task HandleMessageAsync(ProtocolMessage message, IRemoteProtocolAgent agent)
		{
			if (plugins.TryGetValue(message.Domain, out var plugin))
			{
				logger.Log(LogLevel.Debug, MessageHandledID, "Message handled by {PluginType} (domain: {Domain})", plugin.GetType().FullName, plugin.GetDomain());

				IPluginRemoteAgent? pluginAgent;
				if (agentsCache.TryGetValue((agent, plugin), out pluginAgent) == false)
				{
					pluginAgent = new PluginRemoteAgent(agent, plugin.GetDomain());
					agentsCache.Add((agent, plugin), pluginAgent);
				}

				var pluginProtocolMessage = new PluginProtocolMessage(message.Body, message.Attachment is null ? null : new PluginProtocolMessage.MessageAttachment(message.Attachment.Name, message.Attachment.CopyDelegate, message.Attachment.Length));
				await plugin.ProcessMessageAsync(pluginProtocolMessage, new MessageContext(pluginAgent));
			}
			else
			{
				logger.Log(LogLevel.Warning, UnknownMessageDomainID, "Unknown message domain - {Message}", message);
			}
		}
	}
}

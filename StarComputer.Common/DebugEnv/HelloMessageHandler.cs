using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Protocol;

namespace StarComputer.Common.DebugEnv
{
	public class HelloMessageHandler : IMessageHandler
	{
		public Task HandleMessageAsync(ProtocolMessage message, IRemoteProtocolAgent agent)
		{
			Console.WriteLine(message);

			//if (message.Body is string str)
			//{
			//	var newMessage = new ProtocolMessage(message.Domain, str + "+", message.Attachments?.Values, message.DebugMessage + "\nModified by HelloMessageHandler");
			//	await agent.SendMessageAsync(newMessage);
			//}
			//else await agent.SendMessageAsync(message);

			return Task.CompletedTask;
		}
	}
}

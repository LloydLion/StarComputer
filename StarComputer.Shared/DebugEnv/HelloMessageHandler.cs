using Newtonsoft.Json;
using StarComputer.Shared.Protocol;

namespace StarComputer.Shared.DebugEnv
{
	public class HelloMessageHandler : IMessageHandler
	{
		public async Task<SendStatusCode> HandleMessageAsync(ProtocolMessage message, RemoteProtocolAgent agent)
		{
			Console.WriteLine(JsonConvert.SerializeObject(message.Body));

			//if (message.Body is string str)
			//{
			//	var newMessage = new ProtocolMessage(message.Domain, str + "+", message.Attachments?.Values, message.DebugMessage + "\nModified by HelloMessageHandler");
			//	await agent.SendMessageAsync(newMessage);
			//}
			//else await agent.SendMessageAsync(message);

			return SendStatusCode.Successful;
		}
	}
}

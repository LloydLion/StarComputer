using Newtonsoft.Json;
using StarComputer.Shared.Protocol;

namespace StarComputer.Shared.DebugEnv
{
	public class HelloMessageHandler : IMessageHandler
	{
		public Task<SendStatusCode> HandleMessageAsync(ProtocolMessage message, RemoteProtocolAgent agent)
		{
			Console.WriteLine(JsonConvert.SerializeObject(message.Body));

			if (message.Body is string str)
			{
				var newMessage = new ProtocolMessage(message.Domain, str + "+", message.Attachments?.Values, message.DebugMessage + "\nModified by HelloMessageHandler");
				agent.SendMessageAsync(newMessage);		
			}
			else
			{
				agent.SendMessageAsync(message);
			}

			return Task.FromResult(SendStatusCode.Successful);
		}
	}
}

using StarComputer.Common.Abstractions.Connection;
using StarComputer.Server.Abstractions;

namespace StarComputer.Server.DebugEnv
{
	public class GugApprovalAgent : IClientApprovalAgent
	{
		public Task<ClientApprovalResult?> ApproveClientAsync(ClientConnectionInformation clientInformation)
		{
			return Task.FromResult((ClientApprovalResult?)new ClientApprovalResult(s => { }, () => { }));
		}
	}
}

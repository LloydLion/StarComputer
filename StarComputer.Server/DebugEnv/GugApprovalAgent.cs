using StarComputer.Shared.Connection;

namespace StarComputer.Server.DebugEnv
{
	internal class GugApprovalAgent : IClientApprovalAgent
	{
		public Task<ClientApprovalResult?> ApproveClientAsync(ClientConnectionInformation clientInformation)
		{
			return Task.FromResult((ClientApprovalResult?)new ClientApprovalResult(s => { }, () => { }));
		}
	}
}

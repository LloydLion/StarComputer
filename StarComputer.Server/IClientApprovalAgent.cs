using StarComputer.Shared.Connection;

namespace StarComputer.Server
{
	public interface IClientApprovalAgent
	{
		public Task<ClientApprovalResult?> ApproveClientAsync(ClientConnectionInformation clientInformation);
	}
}

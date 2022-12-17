using StarComputer.Common.Abstractions.Connection;

namespace StarComputer.Server.Abstractions
{
	public interface IClientApprovalAgent
	{
		public Task<ClientApprovalResult?> ApproveClientAsync(ClientConnectionInformation clientInformation);
	}
}

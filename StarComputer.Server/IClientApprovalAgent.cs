namespace StarComputer.Server
{
	internal interface IClientApprovalAgent
	{
		public Task<ClientApprovalResult?> ApproveClientAsync(ClientConnectionInformation clientInformation);
	}
}

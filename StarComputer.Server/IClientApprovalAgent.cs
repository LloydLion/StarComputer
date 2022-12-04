namespace StarComputer.Server
{
	internal interface IClientApprovalAgent
	{
		public Task<bool> ApproveClientAsync(ClientApprovalInformation clientInformation);
	}
}

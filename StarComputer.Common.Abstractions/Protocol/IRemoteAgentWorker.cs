namespace StarComputer.Common.Abstractions.Protocol
{
	public interface IRemoteAgentWorker
	{
		public void HandleError(IRemoteProtocolAgent agent, Exception ex);

		public void HandleDisconnect(IRemoteProtocolAgent agent);

		public void ScheduleReconnect(IRemoteProtocolAgent agent);

		public void DispatchMessage(IRemoteProtocolAgent agent, ProtocolMessage message);
	}
}

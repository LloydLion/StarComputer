namespace StarComputer.Shared.Protocol
{
	public interface IRemoteAgentWorker
	{
		public void HandleError(RemoteProtocolAgent agent, Exception ex);

		public void HandleDisconnect(RemoteProtocolAgent agent);

		public void ScheduleReconnect(RemoteProtocolAgent agent);

		public void DispatchMessage(RemoteProtocolAgent agent, ProtocolMessage message);
	}
}

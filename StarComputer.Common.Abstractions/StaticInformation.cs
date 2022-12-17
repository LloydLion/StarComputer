namespace StarComputer.Common.Abstractions
{
	public static class StaticInformation
	{
		public const int ConnectionPort = 623;
		public static readonly PortRange OperationsPortRange = new(624, 644);

#if DEBUG
		public const int ClientConnectTimeout = ServerReconnectionPrepareTimeout + 120000;
		public const int ClientMessageSendTimeout = 30000;
		public const int ServerReconnectionPrepareTimeout = 5000;
#else
		public const int ClientConnectTimeout = ServerReconnectionPrepareTimeout + 5000;
		public const int ClientMessageSendTimeout = 1000;
		public const int ServerReconnectionPrepareTimeout = 5000;
#endif
	}
}

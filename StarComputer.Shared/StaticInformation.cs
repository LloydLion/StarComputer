namespace StarComputer.Shared
{
	public static class StaticInformation
	{
		public const int ConnectionPort = 623;

#if DEBUG
		public const int ClientConnectTimeout = 120000;
#else
		public const int ClientConnectTimeout = 5000;
#endif

#if DEBUG
		public const int ClientMessageSendTimeout = 30000;
#else
		public const int ClientMessageSendTimeout = 1000;
#endif

		public static readonly PortRange OperationsPortRange = new(624, 644);
	}
}

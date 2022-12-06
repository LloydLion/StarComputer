namespace StarComputer.Shared
{
	public static class StaticInformation
	{
		public const int ConnectionPort = 623;

		public const int ClientConnectTimeout = 5000;

		public static readonly PortRange OperationsPortRange = new(624, 644);
	}
}

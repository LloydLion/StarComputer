#define DISABLE_LONG_TIMEOUTS

namespace StarComputer.Common.Abstractions
{
	public static class StaticInformation
	{
		public const int OperationPort = 1623;
		public const string ServerHttpAddressTemplate = "http://{Interface}/starComputerServer/";
		public const string ClientHttpAddressTemplate = "http://{Interface}/starComputerClient/";

#if DEBUG && !DISABLE_LONG_TIMEOUTS
		public const int ClientMessageSendTimeout = 30000;
#else
		public const int ClientMessageSendTimeout = 1000;
#endif
	}
}

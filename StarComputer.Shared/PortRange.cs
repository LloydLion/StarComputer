namespace StarComputer.Shared
{
	public record struct PortRange(int StartPort, int EndPort)
	{
		public bool InRange(int port)
		{
			return StartPort <= port && port < EndPort;
		}
	}
}

namespace StarComputer.Common.Abstractions
{
	public record struct PortRange(int StartPort, int EndPort)
	{
		public int Count => EndPort - StartPort;


		public bool InRange(int port)
		{
			return StartPort <= port && port < EndPort;
		}

		public IEnumerator<int> GetEnumerator()
		{
			for (int i = StartPort; i < EndPort; i++)
				yield return i;
		}
	}
}

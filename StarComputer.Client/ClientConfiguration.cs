using System.Reflection;

namespace StarComputer.Client
{
	internal class ClientConfiguration
	{
		public Version TargetProtocolVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version ?? throw new NullReferenceException();
	}
}

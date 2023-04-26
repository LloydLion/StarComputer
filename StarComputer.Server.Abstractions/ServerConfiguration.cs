using StarComputer.Common;
using StarComputer.Common.Abstractions;
using System.Net;
using System.Reflection;

namespace StarComputer.Server.Abstractions
{
	public class ServerConfiguration
	{
#if !DEBUG
		private bool isPasswordSetted = false;
#endif
		private string serverPassword = "DEBUG PASSWORD";


		public string ServerHttpAddressTemplate { get; set; } = StaticInformation.ServerHttpAddressTemplate;

		public IPEndPoint Interface { get; set; } = IPEndPoint.Parse("127.0.0.1:" + StaticInformation.OperationPort);

		public Version TargetProtocolVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version ?? throw new NullReferenceException();
		
		public string ServerPassword
		{
			get => serverPassword;

			set
			{
				serverPassword = value;
#if !DEBUG
				isPasswordSetted = true;
#endif
			}
		}


		public void Validate()
		{
#if !DEBUG
			if (isPasswordSetted == false)
				throw new ArgumentException($"Password must be setted in non debug mode");
#endif
		}

		public string ConstructServerHttpAddress() => ServerHttpAddressTemplate.Replace("{Interface}", Interface.ToString());
	}
}
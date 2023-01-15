using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;
using System;

namespace StarComputer.Client.Abstractions
{
	public interface IClientProtocolEnviroment : IProtocolEnvironment
	{
		public IClient Client { get; }


		public event Action ClientConnected;

		public event Action ClientDisconnected;
	}
}

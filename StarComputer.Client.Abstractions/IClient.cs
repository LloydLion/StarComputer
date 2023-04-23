﻿using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Client.Abstractions
{
	public interface IClient
	{
		public bool IsConnected { get; }


		public event Action ConnectionStatusChanged;


		public ValueTask TerminateAsync();

		public ValueTask ConnectAsync(ConnectionConfiguration connectionConfiguration);

		public ValueTask CloseAsync();

		public IRemoteProtocolAgent GetServerAgent();

		public ConnectionConfiguration GetConnectionConfiguration();

		public void MainLoop(IPluginStore plugins);
	}
}

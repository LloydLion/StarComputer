﻿using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Client.Abstractions
{
	public interface IClient
	{
		public bool IsConnected { get; }

		public bool IsTerminated { get; }


		public event EventHandler ConnectionStatusChanged;


		public ValueTask TerminateAsync();

		public ValueTask ConnectAsync(ConnectionConfiguration connectionConfiguration);

		public ValueTask CloseAsync();

		public IRemoteProtocolAgent GetServerAgent();

		public ConnectionConfiguration GetConnectionConfiguration();

		public ClientConfiguration GetConfiguration();

		public void MainLoop(JoinKeyCollection joinKeys);
	}
}

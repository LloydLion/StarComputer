using StarComputer.Client.Abstractions;
using StarComputer.Client.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Protocol;
using StarComputer.Common.Plugins.Protocol;

namespace StarComputer.Client
{
	public class PluginClient : IPluginClient
	{
		private readonly IClient client;
		private readonly PluginDomain targetPluginDomain;
		private readonly ConnectionHandler handler;


		public PluginClient(IClient client, PluginDomain targetPluginDomain)
		{
			this.client = client;
			this.targetPluginDomain = targetPluginDomain;
			handler = new(client);
		}


		public bool IsConnected => client.IsConnected;


		public ConnectionConfiguration GetConnectionConfiguration()
		{
			throw new NotImplementedException();
		}

		public IPluginRemoteAgent GetServerAgent()
		{
			return new PluginRemoteAgent(client.GetServerAgent(), targetPluginDomain);
		}



		public event Action? ClientConnected
		{ add => handler.ConnectHandler += value; remove => handler.ConnectHandler -= value; }

		public event Action? ClientDisconnected
		{ add => handler.DisconnectHandler += value; remove => handler.DisconnectHandler -= value; }


		private class ConnectionHandler
		{
			private readonly IClient client;

			private Action? connectHandler;
			private Action? disconnectHandler;
			private bool isAttached = false;


			public Action? ConnectHandler { get => connectHandler; set { connectHandler = value; NotifyChanged(); } }

			public Action? DisconnectHandler { get => disconnectHandler; set { disconnectHandler = value; NotifyChanged(); } }


			public ConnectionHandler(IClient client)
			{
				this.client = client;
			}


			public void OnConnectionStatusChanged()
			{
				if (client.IsConnected)
					ConnectHandler?.Invoke();
				else DisconnectHandler?.Invoke();
			}

			private void NotifyChanged()
			{
				if (isAttached == false && (connectHandler is not null || disconnectHandler is not null))
				{
					isAttached = true;
					client.ConnectionStatusChanged += OnConnectionStatusChanged;
				}
				else if (isAttached && connectHandler is null && disconnectHandler is null)
				{
					isAttached = false;
					client.ConnectionStatusChanged -= OnConnectionStatusChanged;
				}
			}
		}
	}
}
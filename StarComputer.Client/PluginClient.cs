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
			handler = new(client, this);
		}


		public bool IsConnected => client.IsConnected;


		public ConnectionConfiguration GetConnectionConfiguration()
		{
			return client.GetConnectionConfiguration();
		}

		public IPluginRemoteAgent GetServerAgent()
		{
			return new PluginRemoteAgent(client.GetServerAgent(), targetPluginDomain);
		}


		public event EventHandler? ClientConnected
		{ add => handler.ConnectHandler += value; remove => handler.ConnectHandler -= value; }

		public event EventHandler? ClientDisconnected
		{ add => handler.DisconnectHandler += value; remove => handler.DisconnectHandler -= value; }


		private class ConnectionHandler
		{
			private readonly IClient client;
			private readonly PluginClient owner;

			private EventHandler? connectHandler;
			private EventHandler? disconnectHandler;
			private bool isAttached = false;


			public EventHandler? ConnectHandler { get => connectHandler; set { connectHandler = value; NotifyChanged(); } }

			public EventHandler? DisconnectHandler { get => disconnectHandler; set { disconnectHandler = value; NotifyChanged(); } }


			public ConnectionHandler(IClient client, PluginClient owner)
			{
				this.client = client;
				this.owner = owner;
			}


			public void OnConnectionStatusChanged(object? sender, EventArgs e)
			{
				if (client.IsConnected)
					ConnectHandler?.Invoke(owner, EventArgs.Empty);
				else DisconnectHandler?.Invoke(owner, EventArgs.Empty);
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
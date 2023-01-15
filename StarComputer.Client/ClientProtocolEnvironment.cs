using StarComputer.Client.Abstractions;

namespace StarComputer.Client
{
	public class ClientProtocolEnvironment : IClientProtocolEnviroment
	{
		private readonly ConnectionHandler handler;


		public ClientProtocolEnvironment(IClient client)
		{
			Client = client;
			handler = new(client);
		}


		public IClient Client { get; }


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

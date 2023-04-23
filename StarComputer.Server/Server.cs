using Microsoft.Extensions.Options;
using StarComputer.Server.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using System.Net;
using StarComputer.Common.Abstractions.Threading;
using Microsoft.Extensions.Logging;
using System.Text;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using static StarComputer.Common.Protocol.HttpProtocolHelper;
using StarComputer.Common.Protocol;
using StarComputer.Common.Abstractions.Connection;

namespace StarComputer.Server
{
	public class Server : IServer
	{
		private static readonly EventId ServerReadyID = new(10, "ServerReady");
		private static readonly EventId WaitingNewTasksID = new(11, "WaitingNewTasks");
		private static readonly EventId CloseSignalRecivedID = new(12, "CloseSignalRecived");
		private static readonly EventId NewConnectionAcceptedID = new(13, "NewConnectionAccepted");
		private static readonly EventId NewClientAcceptedID = new(14, "NewClientAccepted");
		private static readonly EventId ClientConnectedID = new(15, "ClientConnected");
		private static readonly EventId ClientJoinedID = new(16, "ClientJoined");
		private static readonly EventId ClientRejoinedID = new(17, "ClientRejoined");
		private static readonly EventId ClientConnectionLostID = new(18, "ClientConnectionLost");
		private static readonly EventId ClientDisconnectedID = new(19, "ClientDisconnected");
		private static readonly EventId MessageRecivedID = new(31, "MessageRecived");
		private static readonly EventId ExecutingNewTaskID = new(32, "ExecutingNewTask");
		private static readonly EventId FailedToExecuteTaskID = new(33, "FailedToExecuteTask");
		private static readonly EventId ClientInitializeErrorID = new(21, "ClientInitializeError");
		private static readonly EventId ClientConnectionFailID = new(22, "ClientConnectionFail");
		private static readonly EventId ClientJoinFailID = new(23, "ClientJoinFail");
		private static readonly EventId ClientRejoinFailID = new(24, "ClientRejoinFail");
		private static readonly EventId ProtocolErrorID = new(25, "ProtocolError");


		private readonly IThreadDispatcher<Action> mainThreadDispatcher;
		private readonly ILogger<Server> logger;
		private readonly IMessageHandler messageHandler;
		private readonly IBodyTypeResolver bodyTypeResolver;
		private readonly ServerConfiguration options;
		private readonly HttpListener listener;

		private readonly AutoResetEvent closeRequestEvent = new(false);
		private readonly TaskCompletionSource closeRequestCoevent = new(false);
		private readonly AutoResetEvent listenRequestEvent = new(false);
		private ListenRequest? listenRequest;

		private readonly Dictionary<Guid, ServerSideClientInternal> clients = new();


		public Server(
			IOptions<ServerConfiguration> options,
			IThreadDispatcher<Action> mainThreadDispatcher,
			ILogger<Server> logger,
			IMessageHandler messageHandler,
			IBodyTypeResolver bodyTypeResolver
		)
		{
			options.Value.Validate();
			this.options = options.Value;

			listener = new();
			listener.Prefixes.Add(options.Value.ConstructServerHttpAddress());

			this.mainThreadDispatcher = mainThreadDispatcher;
			this.logger = logger;
			this.messageHandler = messageHandler;
			this.bodyTypeResolver = bodyTypeResolver;
		}


		public bool IsListening { get; private set; }


		public event Action? ListeningStatusChanged;

		public event Action<ServerSideClient>? ClientConnected;

		public event Action<ServerSideClient>? ClientDisconnected;


		public void Close()
		{
			if (IsListening == false)
				throw new InvalidOperationException("Server is already closed, enable to close server twice");

			closeRequestEvent.Set();
			closeRequestCoevent.Task.Wait();
		}

		public ServerSideClient GetClientByAgent(Guid protocolAgentId)
		{
			var ssci = clients[protocolAgentId];
			return new ServerSideClient(ssci.ConnectionInformation, ssci.Agent);
		}

		public IEnumerable<ServerSideClient> ListClients()
		{
			foreach (var ssci in clients.Values)
				yield return new ServerSideClient(ssci.ConnectionInformation, ssci.Agent);
		}

		public ValueTask ListenAsync()
		{
			if (IsListening == true)
				throw new InvalidOperationException("Server is already listening, enable to open server twice");

			var tcs = new TaskCompletionSource();
			listenRequest = new(tcs);
			listenRequestEvent.Set();
			return new ValueTask(tcs.Task);
		}

		public void MainLoop(IPluginStore plugins)
		{
		restart:
			listenRequestEvent.WaitOne();
			if (listenRequest is null) goto restart;
			try
			{
				listener.Start();
			}
			catch (Exception ex)
			{
				listenRequest.Task.SetException(ex);
				goto restart;
			}

			IsListening = true;
			ListeningStatusChanged?.Invoke();

			var httpContextAsyncResult = listener.BeginGetContext(null, null);

			var handlers = new WaitHandle[] { closeRequestEvent, httpContextAsyncResult.AsyncWaitHandle };

			while (IsListening)
			{
				var waitResult = mainThreadDispatcher.WaitHandles(handlers);

				CheckClientsTimeout();

				if (waitResult == ThreadDispatcherStatic.ClosedIndex)
				{
					try
					{
						listener.Close();

						foreach (var client in clients.Values)
							client.Agent.Disconnect();
						clients.Clear();

						IsListening = false;
						ListeningStatusChanged?.Invoke();
					}
					catch (Exception) { }

					throw new InvalidOperationException("Dispatcher was closed, by external command. Terminating server");
				}
				else if (waitResult == 0) //Closed
				{
					try
					{
						listener.Close();
						IsListening = false;
						ListeningStatusChanged?.Invoke();
						closeRequestCoevent.SetResult();
					}
					catch (Exception ex)
					{
						closeRequestCoevent.SetException(ex);
					}

					return;
				}
				else if (waitResult == ThreadDispatcherStatic.NewTaskIndex)
				{
					var flag = true;
					while (flag)
					{
						try
						{
							logger.Log(LogLevel.Trace, ExecutingNewTaskID, "Server started to execute new task");
							flag = mainThreadDispatcher.ExecuteTask();
						}
						catch (Exception ex)
						{
							logger.Log(LogLevel.Error, FailedToExecuteTaskID, ex, "Failed to execute some task");
						}
					}
				}
				else if (waitResult == 1) //HTTP context
				{
					var context = listener.EndGetContext(httpContextAsyncResult);

					httpContextAsyncResult = listener.BeginGetContext(null, null);
					handlers[1] = httpContextAsyncResult.AsyncWaitHandle;

					ProcessHttpMessageAsync(context);
				}
			}
		}

		private void CheckClientsTimeout()
		{
			List<Guid>? clientsToClose = null;
			foreach (var client in clients)
			{
				//if ((DateTime.UtcNow - client.Value.LastHeartbeat).TotalSeconds >= 15)
				//{
				//	clientsToClose ??= new();
				//	clientsToClose.Add(client.Key);
				//}
			}

			if (clientsToClose is not null)
				foreach (var client in clientsToClose)
					DisconnectClient(client);
		}

		private async void ProcessHttpMessageAsync(HttpListenerContext context)
		{
			try
			{
				var headers = context.Request.Headers;
				var typeRaw =  headers[RequestTypeHeader];
				if (typeRaw is null || Enum.TryParse<ServerRequestType>(typeRaw, ignoreCase: true, out var type) == false) throw new BadRequestException($"No {RequestTypeHeader} header in request or it is invalid");

				switch (type)
				{
					case ServerRequestType.Connect:
						await ProcessClientConnectAsync(context);
						break;
					case ServerRequestType.Heartbeat:
						ProcessClientHeartbeat(context);
						break;
					case ServerRequestType.Message:
						await ProcessClientMessageAsync(context);
						break;
					case ServerRequestType.Reset:
						break;
				}
			}
			catch (HttpException ex)
			{
				context.Response.StatusCode = (int)ex.StatusCode;
				context.Response.ContentType = "text/plain; charset=UTF-8";
				await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(ex.Message));
			}
			catch (Exception ex)
			{
				context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				context.Response.ContentType = "text/plain; charset=UTF-8";
				await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(ex.Message));
			}
			finally
			{
				context.Response.Close();
			}
		}

		private async ValueTask ProcessClientConnectAsync(HttpListenerContext context)
		{
			var headers = context.Request.Headers;
			var password = headers[ConnectionPasswordHeader];

			if (password != options.ServerPassword)
				throw new HttpException(HttpStatusCode.Forbidden, "Invalid server password in connection request");
				
			var login = headers[ConnectionLoginHeader];
			var addressRaw = headers[CallbackAddressHeader];

			if (login is null)
				throw new BadRequestException("No client login in connection request");
			if (addressRaw is null)
				throw new BadRequestException("No client callback address in connection request");
			var address = new Uri(addressRaw);

			try
			{
				var localClient = new HttpClient();

				var message = new HttpRequestMessage() { RequestUri = address };
				message.Headers.Add(RequestTypeHeader, ClientRequestType.Ping.ToString());
				var result = await localClient.SendAsync(message);
				if (result.IsSuccessStatusCode == false) throw new Exception();
			}
			catch (Exception)
			{
				throw new BadRequestException("Server tried to call given callback address and did not get success result");
			}

			var agent = new RemoteProtocolAgent(address, bodyTypeResolver, mainThreadDispatcher, ClientRequestType.Message.ToString());
			var uniqueClientID = agent.UniqueAgentId;

			clients.Add(uniqueClientID, new(agent, new(login, address)));

			agent.Start();

			ClientConnected?.Invoke(GetClientByAgent(uniqueClientID));

			PasteClientUniqueID(context.Response.Headers, uniqueClientID);

			context.Response.StatusCode = (int)HttpStatusCode.OK;
		}

		private async ValueTask ProcessClientMessageAsync(HttpListenerContext context)
		{
			var guid = FetchClientUniqueID(context);

			if (clients.TryGetValue(guid, out var client))
			{
				client.ResetHeartbeatTimeout();

				var message = await ParseMessageAsync(context, bodyTypeResolver);

				await messageHandler.HandleMessageAsync(message, client.Agent);

				context.Response.StatusCode = (int)HttpStatusCode.OK;
			}
			else throw new HttpException(HttpStatusCode.Unauthorized, "Client with given id not found");
		}

		private void ProcessClientHeartbeat(HttpListenerContext context)
		{
			var guid = FetchClientUniqueID(context);

			if (clients.TryGetValue(guid, out var client))
			{
				client.ResetHeartbeatTimeout();
				context.Response.StatusCode = (int)HttpStatusCode.OK;
			}
			else throw new HttpException(HttpStatusCode.Unauthorized, "Client with given id not found");
		}

		private void DisconnectClient(Guid clientId)
		{
			clients.Remove(clientId, out var client);
			if (client is null)
				throw new NullReferenceException();
			client.Agent.Disconnect();
			ClientDisconnected?.Invoke(new ServerSideClient(client.ConnectionInformation, client.Agent));
		}


		private record ListenRequest(TaskCompletionSource Task);

		private record ServerSideClientInternal(IRemoteProtocolAgent Agent, ClientConnectionInformation ConnectionInformation)
		{
			public DateTime LastHeartbeat { get; private set; } = DateTime.UtcNow;


			public void ResetHeartbeatTimeout() => LastHeartbeat = DateTime.UtcNow;
		}
	}
}

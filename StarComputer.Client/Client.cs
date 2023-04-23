using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarComputer.Common.Protocol;
using StarComputer.Common.Abstractions.Utils;
using System.Net;
using System.Net.Sockets;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Connection;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using System.Diagnostics.CodeAnalysis;
using static StarComputer.Common.Protocol.HttpProtocolHelper;
using System.Text;
using System.ComponentModel;

namespace StarComputer.Client
{
	public class Client : IClient
	{
		private static readonly EventId ClientReadyID = new(10, "ClientReady");
		private static readonly EventId WaitingNewTasksID = new(11, "WaitingNewTasks");
		private static readonly EventId CloseSignalRecivedID = new(12, "CloseSignalRecived");
		private static readonly EventId NewConnectionToServerID = new(13, "NewConnectionToServer");
		private static readonly EventId ClientDataSentID = new(14, "ClientDataSent");
		private static readonly EventId ConnectionAsClientID = new(15, "ConnectionAsClient");
		private static readonly EventId ClientJoinedID = new(16, "ClientJoined");
		private static readonly EventId ClientRejoinedID = new(17, "ClientRejoined");
		private static readonly EventId ConnectionLostID = new(18, "ClientConnectionLost");
		private static readonly EventId DisconnectedID = new(19, "Disconnected");
		private static readonly EventId MessageRecivedID = new(31, "MessageRecived");
		private static readonly EventId ExecutingNewTaskID = new(32, "ExecutingNewTask");
		private static readonly EventId FailedToExecuteTaskID = new(33, "FailedToExecuteTask");
		private static readonly EventId DebugMessageFromServerID = new(34, "DebugMessageFromServer");
		private static readonly EventId ClientConnectionFailID = new(21, "ClientConnectionFail");
		private static readonly EventId ClientJoinFailID = new(22, "ClientJoinFail");
		private static readonly EventId ClientRejoinFailID = new(23, "ClientRejoinFail");
		private static readonly EventId ProtocolErrorID = new(24, "ProtocolError");


		private readonly ClientConfiguration options;
		private readonly IMessageHandler messageHandler;
		private readonly ILogger<Client> logger;
		private readonly IThreadDispatcher<Action> mainThreadDispatcher;
		private readonly IBodyTypeResolver bodyTypeResolver;
		private readonly HttpListener listener;
		private readonly HttpClient httpClient = new();
		private readonly string callbackUri;

		private ClientConnectTask? clientConnectTask;
		private readonly AutoResetEvent onClientConnectRequested = new(false);
		private ClientCloseTask? clientCloseTask;
		private readonly AutoResetEvent onClientCloseRequested = new(false);

		private Connection? currentConnection;
		private bool isTerminating = false;


		public Client(
			IOptions<ClientConfiguration> options,
			IThreadDispatcher<Action> mainThreadDispatcher,
			ILogger<Client> logger,
			IMessageHandler messageHandler,
			IBodyTypeResolver bodyTypeResolver)
		{
			this.options = options.Value;
			this.messageHandler = messageHandler;
			this.logger = logger;
			this.mainThreadDispatcher = mainThreadDispatcher;
			this.bodyTypeResolver = bodyTypeResolver;

			listener = new();
			callbackUri = options.Value.ConstructClientHttpAddress();
			listener.Prefixes.Add(callbackUri);
		}


		public bool IsConnected => CurrentConnection is not null;

		[MemberNotNullWhen(true, nameof(IsConnected))]
		private Connection? CurrentConnection { get => currentConnection; set { currentConnection = value; ConnectionStatusChanged?.Invoke(); } }


		public event Action? ConnectionStatusChanged;


		public ValueTask TerminateAsync()
		{
			isTerminating = true;

			if (IsConnected) return CloseAsync();
			else
			{
				onClientConnectRequested.Set();
				return ValueTask.CompletedTask;
			}
		}

		public ValueTask CloseAsync()
		{
			if (IsConnected == false)
				throw new InvalidOperationException("Enable to close closed client");

			var tcs = new TaskCompletionSource();
			clientCloseTask = new ClientCloseTask(tcs);
			onClientCloseRequested.Set();
			return new(tcs.Task);
		}

		public ValueTask ConnectAsync(ConnectionConfiguration connectionConfiguration)
		{
			if (IsConnected)
				throw new InvalidOperationException("Client already connected, close client before connect again");

			var tcs = new TaskCompletionSource();
			clientConnectTask = new ClientConnectTask(tcs, connectionConfiguration);
			onClientConnectRequested.Set();
			return new(tcs.Task);
		}

		public ConnectionConfiguration GetConnectionConfiguration()
		{
			if (CurrentConnection is null)
				throw new InvalidOperationException("Enable to get configuration of closed client");
			return CurrentConnection.Configuration;
		}

		public IRemoteProtocolAgent GetServerAgent()
		{
			if (CurrentConnection is null)
				throw new InvalidOperationException("Enable to get server agent of closed client");
			return CurrentConnection.ServerAgent;
		}

		public void MainLoop(IPluginStore plugins)
		{
			listener.Start();

			while (isTerminating == false)
			{
				onClientConnectRequested.WaitOne();
				if (isTerminating) break;

				if (clientConnectTask is null) continue;

				try
				{
					CurrentConnection = FormConnection(clientConnectTask.Configuration);
					clientConnectTask.Task.SetResult();
				}
				catch (Exception ex)
				{
					clientConnectTask.Task.SetException(ex);
					continue;
				}


				var httpContextAsyncResult = listener.BeginGetContext(null, null);

				var handlers = new WaitHandle[] { onClientCloseRequested, httpContextAsyncResult.AsyncWaitHandle };

				while (IsConnected)
				{
					var waitResult = mainThreadDispatcher.WaitHandles(handlers, 2000);

					if (waitResult == ThreadDispatcherStatic.TimeoutIndex)
					{
						var isOK = SendHeartbeat();
						if (isOK == false)
						{
							CurrentConnection!.ServerAgent.Disconnect();
							CurrentConnection = null;
						}
					}
					else if (waitResult == ThreadDispatcherStatic.ClosedIndex)
					{
						try
						{
							listener.Close();
						}
						catch (Exception) { }

						throw new InvalidOperationException("Dispatcher was closed, by external command. Terminating server");
					}
					else if (waitResult == 0) //Closed
					{
						if (clientCloseTask is null) continue;

						try
						{
							CurrentConnection!.ServerAgent.Disconnect();
							CurrentConnection = null;
							clientCloseTask.Task.SetResult();
						}
						catch (Exception ex)
						{
							clientCloseTask.Task.SetException(ex);
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

			listener.Stop();
		}

		private bool SendHeartbeat()
		{
			if (CurrentConnection is null)
				throw new NullReferenceException();

			var serverEndpoint = new Uri(CurrentConnection.Configuration.ConstructServerHttpAddress());

			var message = new HttpRequestMessage();

			message.Headers.Add(RequestTypeHeader, ServerRequestType.Heartbeat.ToString());
			PasteClientUniqueID(message.Headers, CurrentConnection.UniqueID);
			message.RequestUri = serverEndpoint;

			int failCounter = 0;
			try
			{
				var result = httpClient.Send(message);
				if (result.IsSuccessStatusCode == false)
					throw new Exception($"(Heartbeat) Server returned non successful status code [{result.StatusCode}]: " + result.Content.ReadAsStringAsync().Result);
			}
			catch (Exception ex)
			{
				logger.Log(LogLevel.Error, ex, "Heartbeat error");

				failCounter++;

				if (failCounter > 10)
					return false;
			}

			return true;
		}

		private Connection FormConnection(ConnectionConfiguration configuration)
		{
			var serverEndpoint = new Uri(configuration.ConstructServerHttpAddress());

			var message = new HttpRequestMessage();

			message.Headers.Add(RequestTypeHeader, ServerRequestType.Connect.ToString());
			message.Headers.Add(ConnectionPasswordHeader, configuration.ServerPassword);
			message.Headers.Add(ConnectionLoginHeader, configuration.Login);
			message.Headers.Add(CallbackAddressHeader, callbackUri);
			message.RequestUri = serverEndpoint;

			var asyncResult = listener.BeginGetContext((result) =>
			{
				var context = listener.EndGetContext(result);

				var headers = context.Request.Headers;
				var typeRaw = headers[RequestTypeHeader];
				if (typeRaw is null || Enum.TryParse<ClientRequestType>(typeRaw, ignoreCase: true, out var type) == false || type != ClientRequestType.Ping)
					context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				else context.Response.StatusCode = (int)HttpStatusCode.OK;

				context.Response.Close();
			}, null);

			var result = httpClient.Send(message);

			if (result.IsSuccessStatusCode == false)
				throw new Exception($"Server returned non successful status code [{result.StatusCode}]: " + result.Content.ReadAsStringAsync().Result);

			var uniqueID = FetchClientUniqueID(result);

			var agent = new RemoteProtocolAgent(serverEndpoint, bodyTypeResolver, mainThreadDispatcher, ServerRequestType.Message.ToString(), uniqueID);
			var connection = new Connection(configuration, agent, uniqueID);

			return connection;
		}

		private async void ProcessHttpMessageAsync(HttpListenerContext context)
		{
			try
			{
				var headers = context.Request.Headers;
				var typeRaw = headers[RequestTypeHeader];
				if (typeRaw is null || Enum.TryParse<ClientRequestType>(typeRaw, ignoreCase: true, out var type) == false)
					throw new BadRequestException($"No {RequestTypeHeader} header in request or it is invalid");

				switch (type)
				{
					case ClientRequestType.Message:
						await ProcessServerMessageAsync(context);
						break;
					case ClientRequestType.Ping:
						context.Response.StatusCode = (int)HttpStatusCode.OK;
						break;
					case ClientRequestType.Reset:
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

		private async ValueTask ProcessServerMessageAsync(HttpListenerContext context)
		{
			var message = await ParseMessageAsync(context, bodyTypeResolver);

			await messageHandler.HandleMessageAsync(message, CurrentConnection?.ServerAgent ?? throw new NullReferenceException());

			context.Response.StatusCode = (int)HttpStatusCode.OK;
		}


		private record ClientConnectTask(TaskCompletionSource Task, ConnectionConfiguration Configuration);

		private record ClientCloseTask(TaskCompletionSource Task);

		private record Connection(ConnectionConfiguration Configuration, IRemoteProtocolAgent ServerAgent, Guid UniqueID);
	}
}

using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Protocol;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using StarComputer.Server.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace StarComputer.PluginDevelopmentKit
{
	public abstract class PluginBase : IPlugin
	{
	#if DEBUG
		public const int DefaultResponceTimeout = 300000;
	#else
		public const int DefaultResponceTimeout = 10000;
	#endif

		public const string ClientUserNamePrefix = "@";
		public const string ServerUserName = "Master";


		private delegate ValueTask ServerSideMessageProcessor(IServerProtocolEnvironment environment, PluginProtocolMessage message, MessageContext messageContext);

		private delegate ValueTask ClientSideMessageProcessor(IClientProtocolEnviroment environment, PluginProtocolMessage message, MessageContext messageContext);


		private readonly Dictionary<Type, ServerSideMessageProcessor> serverSideProcessors = new();
		private readonly Dictionary<Type, ClientSideMessageProcessor> clientSideProcessors = new();
		private readonly IProtocolEnvironment environment;
		private readonly ConcurrentDictionary<Type, ResponseWaitRequest> responseWaitRequestLine = new();
		private ConcurrentDictionary<Guid, PluginUser>? userAssociationTable = null;


		public Version Version { get; }

		protected IProtocolEnvironment RawEnvironment => environment;

		protected bool IsServerSide => RawEnvironment is IServerProtocolEnvironment;

		protected bool IsClientSide => RawEnvironment is IClientProtocolEnviroment;

		protected string CurrentUserName => RawEnvironment is IClientProtocolEnviroment client ? ClientUserNamePrefix + client.Client.GetConnectionConfiguration().Login : ServerUserName;


		public PluginBase(IProtocolEnvironment environment, Version? version = null)
		{
			this.environment = environment;
			Version = version ?? Assembly.GetCallingAssembly().GetName().Version ??
				throw new Exception("Enable to get calling assembly version, fix it or set version in constructor of PluginBase");
		}


		public void Initialize(IBodyTypeResolverBuilder resolverBuilder)
		{
			var type = GetType();
			var bodies = type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
				.Select(s => new { Type = s, Attribute = s.GetCustomAttribute<MessageBodyAttribute>() })
				.Where(s => s.Attribute is not null);

			foreach (var bodyType in bodies)
				resolverBuilder.RegisterAllias(bodyType.Type, bodyType.Attribute?.TypePseudoName!);


			var processors = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
				.Where(s => s.GetCustomAttribute<MessageProcessorAttribute>() is not null);

			foreach (var processor in processors)
				LoadProcessor(processor);


			Initialize();

			if (IfClientSide(out var clientEnviroment))
				Initialize(clientEnviroment);
			else if (IfServerSide(out var serverEnviroment))
				Initialize(serverEnviroment);
		}

		protected virtual void Initialize() { }

		protected virtual void Initialize(IServerProtocolEnvironment serverProtocolEnvironment) { }

		protected virtual void Initialize(IClientProtocolEnviroment clientProtocolEnviroment) { }

		protected PluginUser AssociateUser(IPluginRemoteAgent agent)
		{
			if (userAssociationTable is null)
			{
				userAssociationTable = new();

				//Initial actions
				if (IfServerSide(out var tserverEnvironment))
					tserverEnvironment.Server.ClientDisconnected += (sender, e) =>
					{
						userAssociationTable.TryRemove(e.Client.Agent.UniqueAgentId, out var user);
						user?.Dispose();
					};
				else if (IfClientSide(out var tclientEnviroment))
					tclientEnviroment.Client.ClientDisconnected += (sender, e) =>
					{
						foreach (var item in userAssociationTable.Values)
							item.Dispose();
						userAssociationTable.Clear();
					};
				else throw new Exception("Side is not client or server");
			}


			if (IfServerSide(out var serverEnvironment))
			{
				if (userAssociationTable.TryGetValue(agent.UniqueAgentId, out var user))
					return user;

				if (serverEnvironment.Server.ListClients().Any(s => Equals(s.Agent, agent)))
					return createNewUser(ClientUserNamePrefix + serverEnvironment.Server.GetClientByAgent(agent).ConnectionInformation.Login);
				else throw new ArgumentException("On server side given agent is not present in server's clients");
			}
			else if(IfClientSide(out var clientEnviroment))
			{
				if (userAssociationTable.TryGetValue(agent.UniqueAgentId, out var user))
					return user;

				if (Equals(agent, clientEnviroment.Client.GetServerAgent()))
					return createNewUser(ServerUserName);
				else throw new ArgumentException("On client side given agent is not client's server agent");
			}
			else throw new Exception("Side is not client or server");



			PluginUser createNewUser(string userName)
			{
				var newUser = new PluginUser(agent, userName);
				userAssociationTable.TryAdd(agent.UniqueAgentId, newUser);
				return newUser;
			}
		}

		protected PluginUser GetServerAssociatedUser(IClientProtocolEnviroment clientEnviroment)
		{
			return AssociateUser(clientEnviroment.Client.GetServerAgent());
		}

		public async ValueTask ProcessMessageAsync(PluginProtocolMessage message, MessageContext messageContext)
		{
			if (message.Body is null) return;
			var bodyType = message.Body.GetType();

			if (responseWaitRequestLine.TryGetValue(bodyType, out var rwr))
			{
				responseWaitRequestLine.TryRemove(new(bodyType, rwr));
				await rwr.SetResultAsync(message);
				return;
			}

			if (IfClientSide(out var clientEnviroment))
			{
				if (clientSideProcessors.TryGetValue(bodyType, out var processor))
				{
					await processor(clientEnviroment, message, messageContext);
				}
			}
			else if (IfServerSide(out var serverEnviroment))
			{
				if (serverSideProcessors.TryGetValue(bodyType, out var processor))
				{
					await processor(serverEnviroment, message, messageContext);
				}
			}
		}

		protected async Task<TypedPluginProtocolMessage<TBody>> SendMessageAndRequestResponse<TBody>(IPluginRemoteAgent agent, PluginProtocolMessage message, bool isWaitsAttachment = false) where TBody : class
		{
			var task = RequestResponse<TBody>(isWaitsAttachment);
			await agent.SendMessageAsync(message);
			return await task;
		}

		protected Task<TypedPluginProtocolMessage<TBody>> SendMessageAndRequestResponse<TBody>(IPluginRemoteAgent agent, object messageBody, bool isWaitsAttachment = false) where TBody : class =>
			SendMessageAndRequestResponse<TBody>(agent, new PluginProtocolMessage(messageBody), isWaitsAttachment);

		protected async Task<TypedPluginProtocolMessage<TBody>> RequestResponse<TBody>(bool isWaitsAttachment, TimeSpan? timeout = null) where TBody : class
		{
			timeout ??= TimeSpan.FromMilliseconds(DefaultResponceTimeout);
			var btimeout = timeout.Value;

			var tcs = new TaskCompletionSource<TypedPluginProtocolMessage<TBody>>();
			var expireDate = DateTime.UtcNow + btimeout;
			var request = new ResponseWaitRequest(typeof(TBody), (msg) => tcs.SetResult(TypedPluginProtocolMessage<TBody>.CreateFrom(msg)), expireDate, isWaitsAttachment);

			var timeoutTask = Task.Delay(btimeout);

			responseWaitRequestLine.TryAdd(typeof(TBody), request);

			var result = await Task.WhenAny(tcs.Task, timeoutTask);
			if (result == timeoutTask)
			{
				responseWaitRequestLine.TryRemove(typeof(TBody), out _);
				throw new TimeoutException($"Opposite side is busy and didn't send response of {typeof(TBody)}");
			}
			else return tcs.Task.Result;
		}

		protected IServerProtocolEnvironment ServerOnly([CallerMemberName] string nameOfCallerForException = "Caller name") =>
			(environment as IServerProtocolEnvironment) ?? throw new InvalidCallSideException("Server", nameOfCallerForException);

		protected bool IfServerSide([NotNullWhen(true)] out IServerProtocolEnvironment? environment, [CallerMemberName] string nameOfCallerForException = "Caller name")
		{
			environment = null;
			if (IsServerSide)
				environment = ServerOnly(nameOfCallerForException);
			return IsServerSide;
		}

		protected IClientProtocolEnviroment ClientOnly([CallerMemberName] string nameOfCallerForException = "Caller name") =>
			(environment as IClientProtocolEnviroment) ?? throw new InvalidCallSideException("Client", nameOfCallerForException);

		protected bool IfClientSide([NotNullWhen(true)] out IClientProtocolEnviroment? environment, [CallerMemberName] string nameOfCallerForException = "Caller name")
		{
			environment = null;
			if (IsClientSide)
				environment = ClientOnly(nameOfCallerForException);
			return IsClientSide;
		}

		protected TResult CallSideDependent<TResult>(Func<IServerProtocolEnvironment, TResult> serverDepend, Func<IClientProtocolEnviroment, TResult> clientDepend)
		{
			if (IfClientSide(out var clientEnviroment))
				return clientDepend(clientEnviroment);
			else if (IfServerSide(out var serverEnviroment))
				return serverDepend(serverEnviroment);
			else throw new Exception("Side is not client or server");
		}

		protected void CallSideDependent(Action<IServerProtocolEnvironment> serverDepend, Action<IClientProtocolEnviroment> clientDepend)
		{
			if (IfClientSide(out var clientEnviroment))
				clientDepend(clientEnviroment);
			else if (IfServerSide(out var serverEnviroment))
				serverDepend(serverEnviroment);
			else throw new Exception("Side is not client or server");
		}

		private void LoadProcessor(MethodInfo method)
		{
			var parameters = method.GetParameters();

			if (method.ReturnType != typeof(void) && method.ReturnType != typeof(ValueTask))
				return;

			if (parameters.Length != 4) return;

			var environmentPara = parameters[0];
			var messagePara = parameters[1];
			var msgContextPara = parameters[2];
			var bodyPara = parameters[3];

			if (messagePara.ParameterType != typeof(PluginProtocolMessage)) return;
			if (msgContextPara.ParameterType != typeof(MessageContext)) return;

			var dictionatyKey = bodyPara.ParameterType;

			if (environmentPara.ParameterType == typeof(IServerProtocolEnvironment))
			{
				registerAsServerProcessor();
			}
			else if (environmentPara.ParameterType == typeof(IClientProtocolEnviroment))
			{
				registerAsClientProcessor();
			}
			else if (environmentPara.ParameterType == typeof(IProtocolEnvironment))
			{
				registerAsServerProcessor();
				registerAsClientProcessor();
			}
			else return;



			void registerAsServerProcessor()
			{
				var pdelegate = new ServerSideMessageProcessor((env, msg, ctx) =>
				{
					var result = method.Invoke(this, new[] { env, msg, ctx, msg.Body });
					if (result is null) return ValueTask.CompletedTask;
					else return (ValueTask)result;
				});

				serverSideProcessors.Add(dictionatyKey, pdelegate);
			}
			
			void registerAsClientProcessor()
			{
				var pdelegate = new ClientSideMessageProcessor((env, msg, ctx) =>
				{
					var result = method.Invoke(this, new[] { env, msg, ctx, msg.Body });
					if (result is null) return ValueTask.CompletedTask;
					else return (ValueTask)result;
				});

				clientSideProcessors.Add(dictionatyKey, pdelegate);
			}
		}


		private record ResponseWaitRequest(Type WaitingBodyType, Action<PluginProtocolMessage> SetResultDelegate, DateTime ExpireDate, bool IsWaitsAttachment)
		{
			public async ValueTask SetResultAsync(PluginProtocolMessage message)
			{
				var bodyType = message.Body.GetType();
				if (bodyType is not null && bodyType.IsAssignableTo(WaitingBodyType) == false)
					throw new InvalidOperationException($"Request waiting instance of {WaitingBodyType.FullName} or inheritor. Given type is {bodyType.FullName}");

				if (IsWaitsAttachment && message.Attachment is not null)
				{
					var memory = new MemoryStream(message.Attachment.Length);
					await message.Attachment.CopyDelegate(memory);
					memory.Position = 0;
					var attachment = new PluginProtocolMessage.MessageAttachment(message.Attachment.Name, (stream) => new(memory.CopyToAsync(stream)), message.Attachment.Length);
					message = new PluginProtocolMessage(message.Body, attachment);
				}

				SetResultDelegate(message);
			}
		}

		protected record struct TypedPluginProtocolMessage<TBody>(PluginProtocolMessage Message, TBody Body)
		{
			public static TypedPluginProtocolMessage<TBody> CreateFrom(PluginProtocolMessage message)
			{
				return new TypedPluginProtocolMessage<TBody>(message, (TBody)message.Body);
			}
		}

		[AttributeUsage(AttributeTargets.Class)]
		protected sealed class MessageBodyAttribute : Attribute
		{
			public MessageBodyAttribute(string typePseudoName)
			{
				TypePseudoName = typePseudoName;
			}


			public string TypePseudoName { get; }
		}


		[AttributeUsage(AttributeTargets.Method)]
		protected sealed class MessageProcessorAttribute : Attribute { }

		protected sealed class PluginUser : IServiceProvider, IDisposable
		{
			private readonly Dictionary<Type, object> services = new();


			public PluginUser(IPluginRemoteAgent agent, string userName)
			{
				Agent = agent;
				UserName = userName;
			}


			public IPluginRemoteAgent Agent { get; }

			public string UserName { get; }

			public string ClientLogin
			{
				get
				{
					if (UserName.StartsWith(ClientUserNamePrefix))
						return UserName[ClientUserNamePrefix.Length..];
					else throw new InvalidOperationException("User is not associated with client");
				}
			}


			public void Dispose()
			{
				foreach (var component in services.Values)
					if (component is IDisposable disposable)
						disposable.Dispose();
			}

			public object? GetService(Type serviceType)
			{
				if (services.TryGetValue(serviceType, out var value))
					return value;
				else return null;
			}

			public void RegisterService(object service, Type targetType) =>
				services.Add(targetType, service);
		}

		public class InvalidCallSideException : Exception
		{
			public InvalidCallSideException(string sideName, string member) : base($"Enable to call {member} from non {sideName} side") { }
		}
	}
}

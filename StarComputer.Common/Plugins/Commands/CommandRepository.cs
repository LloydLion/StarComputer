using StarComputer.Common.Abstractions.Plugins.Commands;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace StarComputer.Common.Plugins.Commands
{
	public class CommandRepository : ICommandRepository
	{
		private readonly Dictionary<string, Command> commands = new();


		public Command this[string key] => IfInitialized(() => commands[key]);


		public bool IsInitialized { get; private set; }

		public int Count => IfInitialized(commands.Count);

		public IEnumerable<string> Keys => IfInitialized(commands.Keys);

		public IEnumerable<Command> Values => IfInitialized(commands.Values);


		public void Initialize(IEnumerable<Command> commands)
		{
			if (IsInitialized)
				throw new InvalidOperationException("Repository can be initialized only one time");

			IsInitialized = true;

			foreach (var command in commands)
				this.commands.Add(command.Name, command);
		}

		public bool ContainsKey(string key) => IfInitialized(() => commands.ContainsKey(key));

		public IEnumerator<Command> GetEnumerator() => IfInitialized(commands.Values.GetEnumerator);

		public bool TryGetValue(string key, [MaybeNullWhen(false)] out Command value)
		{
			IfInitialized();
			return commands.TryGetValue(key, out value);
		}

		IEnumerator<KeyValuePair<string, Command>> IEnumerable<KeyValuePair<string, Command>>.GetEnumerator() => IfInitialized(commands.GetEnumerator);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


		private T IfInitialized<T>(Func<T> func)
		{
			IfInitialized();
			return func();
		}

		private T IfInitialized<T>(T value)
		{
			IfInitialized();
			return value;
		}

		private void IfInitialized()
		{
			if (IsInitialized == false)
				throw new InvalidOperationException("Initialize repository before use");
		}
	}
}

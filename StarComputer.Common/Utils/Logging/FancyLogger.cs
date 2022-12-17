using Microsoft.Extensions.Logging;

namespace StarComputer.Common.Utils.Logging
{
	internal class FancyLogger : ILogger
	{
		private readonly Stack<ScopeHandler> scopeHandlers = new();
		private readonly string category;


		public FancyLogger(string category)
		{
			this.category = category;
		}


		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			lock (this)
			{
				var handler = new ScopeHandler(state.ToString(), scopeHandlers);
				scopeHandlers.Push(handler);
				return handler;
			}
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			lock (this)
			{
				var message = formatter(state, exception);

				var now = DateTime.Now;
				var utcNow = DateTime.UtcNow;

				bool direction = now > utcNow;
				TimeSpan delta = (now - utcNow) * (direction ? 1 : -1);
				delta += TimeSpan.FromSeconds(2);
				var formattedDelta = delta.ToString("hh\\:mm");

				var color = logLevel switch
				{
					LogLevel.Trace => ConsoleColor.Gray,
					LogLevel.Debug => ConsoleColor.White,
					LogLevel.Information => ConsoleColor.Blue,
					LogLevel.Warning => ConsoleColor.Yellow,
					LogLevel.Error => ConsoleColor.Red,
					LogLevel.Critical => ConsoleColor.DarkRed,
					LogLevel.None => throw new NotSupportedException(),
					_ => throw new NotSupportedException(),
				};

				lock (Console.Out)
				{
					Print($"{now:g} ({(direction ? '+' : '-')}{formattedDelta}) ", ConsoleColor.Cyan);

					Print($"[from {category}/{eventId.Name} ({eventId.Id})] ", ConsoleColor.Magenta);

					if (scopeHandlers.Count != 0)
					{
						Print($"[", ConsoleColor.White);
						foreach (var scope in scopeHandlers.Take(scopeHandlers.Count - 1))
						{
							Print($"{scope.Scope ?? "*"}", ConsoleColor.Yellow);
							Print($"|", ConsoleColor.White);
						}

						Print($"{scopeHandlers.Last().Scope ?? "*"}", ConsoleColor.Yellow);

						Print($"] ", ConsoleColor.White);
					}

					Print($"[{logLevel}]: ", color);

					Print(message, ConsoleColor.White);


					Console.WriteLine();


					if (exception is not null)
					{
						Print(exception.ToString(), ConsoleColor.Red);
						Console.WriteLine();
					}
				}
			}
		}

		private static void Print(string message, ConsoleColor color)
		{
			var oldColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.Write(message);
			Console.ForegroundColor = oldColor;
		}


		private class ScopeHandler : IDisposable
		{
			private readonly Stack<ScopeHandler> container;


			public ScopeHandler(string? scope, Stack<ScopeHandler> container)
			{
				Scope = scope;
				this.container = container;
			}


			public string? Scope { get; }


			public void Dispose()
			{
				if (container.Peek() != this)
				{
					throw new InvalidOperationException("Invalid order of scopes dispose");
				}

				container.Pop();
			}
		}
	}
}

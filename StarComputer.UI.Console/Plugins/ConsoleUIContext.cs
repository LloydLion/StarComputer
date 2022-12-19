using StarComputer.Common.Abstractions.Plugins.ConsoleUI;
using StarComputer.Common.Abstractions.Utils;
using System.Text;
using SConsole = System.Console;

namespace StarComputer.UI.Console.Plugins
{
	public class ConsoleUIContext : IConsoleUIContext, IDisposable
	{
		private readonly Thread uiThread;
		private readonly ThreadDispatcher<Action> mainThreadDispatcher;
		private readonly List<Action<string>> newLineSentSubs = new();
		private bool isClosing = false;


		public ConsoleUIContext(ThreadDispatcher<Action> mainThreadDispatcher)
		{
			uiThread = new Thread(ThreadHandle);
			uiThread.Start();
			this.mainThreadDispatcher = mainThreadDispatcher;
		}


		public TextWriter Out => SConsole.Out;

		public TextWriter Error => SConsole.Error;

		public bool KeyAvailable => SConsole.KeyAvailable;


		public ConsoleColor ForegroundColor { get => SConsole.ForegroundColor; set => SConsole.ForegroundColor = value; }

		public ConsoleColor BackgroundColor { get => SConsole.BackgroundColor; set => SConsole.BackgroundColor = value; }


		public event Action<string>? NewLineSent
		{
			add => newLineSentSubs.Add(new NewLineSentSubWrap(value ?? throw new NullReferenceException(), mainThreadDispatcher).WrappedSub);
			remove => newLineSentSubs.Remove(new NewLineSentSubWrap(value ?? throw new NullReferenceException(), mainThreadDispatcher).WrappedSub);
		}

		private event Action<string>? NewLineSentUnsync
		{
			add => newLineSentSubs.Add(value ?? throw new NullReferenceException());
			remove => newLineSentSubs.Remove(value ?? throw new NullReferenceException());
		}

		public void Beep() => SConsole.Beep();

		public string ReadLine()
		{
			var result = new string[1];
			var waitEvent = new AutoResetEvent(false);

			NewLineSentUnsync += onNewLine;
			waitEvent.WaitOne();
			NewLineSentUnsync -= onNewLine;

			return result[0];



			void onNewLine(string value)
			{
				result[0] = value;
				waitEvent.Set();
			}
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);

			isClosing = true;
			uiThread.Join();
		}

		private void ThreadHandle()
		{
			var lineBuilder = new StringBuilder();
			while (true)
			{
				while (KeyAvailable == false && isClosing == false)
					Thread.Sleep(1);
				if (isClosing) break;

				var key = SConsole.ReadKey();


				if (key.KeyChar == '\r')
				{
					SConsole.WriteLine();

					foreach (var sub in newLineSentSubs)
					{
						try
						{
							sub?.Invoke(lineBuilder.ToString());
						}
						catch (Exception) { }
					}

					lineBuilder = new();
				}
				else if (key.KeyChar == '\b')
				{
					if (lineBuilder.Length > 0)
						lineBuilder.Remove(lineBuilder.Length - 1, 1);
				}
				else
				{
					lineBuilder.Append(key.KeyChar);
				}
			}
		}


		private class NewLineSentSubWrap
		{
			private readonly Action<string> sub;
			private readonly ThreadDispatcher<Action> dispatcher;


			public NewLineSentSubWrap(Action<string> sub, ThreadDispatcher<Action> dispatcher)
			{
				this.sub = sub;
				this.dispatcher = dispatcher;
			}


			public void WrappedSub(string value)
			{
				dispatcher.DispatchTask(() =>
				{
					sub(value);
				});
			}

			public override bool Equals(object? obj) => obj is NewLineSentSubWrap other && Equals(other.sub, sub) && Equals(other.dispatcher, dispatcher);

			public override int GetHashCode() => HashCode.Combine(sub, dispatcher);
		}
	}
}

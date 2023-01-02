namespace StarComputer.Common.Abstractions.Plugins.UI.Console
{
	public interface IConsoleUIContext : IUIContext
	{
		public TextWriter Out { get; }

		public TextWriter Error { get; }

		public bool KeyAvailable { get; }

		public ConsoleColor ForegroundColor { get; set; }

		public ConsoleColor BackgroundColor { get; set; }


		public string ReadLine();

		public void Beep();


		public event Action<string> NewLineSent;
	}
}

namespace StarComputer.Common.Abstractions.Plugins.ConsoleUI
{
	public interface IConsoleUIContext : IUIContext
	{
		public TextWriter Out { get; }

		public TextWriter Error { get; }

		public TextReader In { get; }

		public bool KeyAvailable { get; }

		public ConsoleColor ForegroundColor { get; set; }

		public ConsoleColor BackgroundColor { get; set; }


		public void Beep();
	}
}

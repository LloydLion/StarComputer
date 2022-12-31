using Avalonia.Controls;
using StarComputer.Common.Abstractions.Plugins.ConsoleUI;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;

namespace StarComputer.Client.UI.Avalonia
{
	public class AvaloniaBasedConsoleUIContext : IConsoleUIContext, INotifyPropertyChanged
	{
		private readonly StringBuilder outputContent = new();
		private readonly ConcurrentQueue<string> linesToSend = new();


		public AvaloniaBasedConsoleUIContext()
		{
			Out = TextWriter.Synchronized(new OutputWriter(outputContent, "", () => PropertyChanged?.Invoke(this, new(nameof(OutputContent)))));
			Error = TextWriter.Synchronized(new OutputWriter(outputContent, "!!ERROR!!: ", () => PropertyChanged?.Invoke(this, new(nameof(OutputContent)))));
		}


		public TextWriter Out { get; }

		public TextWriter Error { get; }

		public bool KeyAvailable => linesToSend.IsEmpty == false;


		public ConsoleColor ForegroundColor { get; set; }

		public ConsoleColor BackgroundColor { get; set; }

		public string OutputContent => outputContent.ToString();


		public event Action<string>? NewLineSent;

		public event PropertyChangedEventHandler? PropertyChanged;


		public void Beep()
		{
			//Some beep
		}

		public string ReadLine()
		{
			string? result;
			while (linesToSend.TryDequeue(out result) == false)
				Thread.Sleep(1);

			return result;
		}

		public void SendNewLine(string newLine)
		{
			linesToSend.Enqueue(newLine);
			NewLineSent?.Invoke(newLine);
			Out.WriteLine("INPUT: " + newLine);
		}


		private class OutputWriter : TextWriter
		{
			private readonly StringBuilder output;
			private readonly string prefix;
			private readonly Action onChangedDelegate;
			private bool isPrependToWritePrefix = false;


			public OutputWriter(StringBuilder output, string prefix, Action onChangedDelegate)
			{
				this.output = output;
				this.prefix = prefix;
				this.onChangedDelegate = onChangedDelegate;
			}


			public override Encoding Encoding => Encoding.Default;

			public override string NewLine => "\r\n";


			public override void Write(char charToWrite)
			{
				if (isPrependToWritePrefix)
					output.Append(prefix);

				output.Append(charToWrite);

				if (charToWrite == NewLine[^1])
					isPrependToWritePrefix = true;

				if (output.Length > 4000)
					output.Remove(0, output.Length - 4000);

				onChangedDelegate.Invoke();
			}
		}
	}
}

using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;

namespace StarComputer.UI.Avalonia
{
	public partial class ErrorDialogView : Window
	{
		private readonly string errorMessage;


		public ErrorDialogView(string errorMessage)
		{
			InitializeComponent();
			if (Design.IsDesignMode == false)
				Activated += OnActivated;

			this.errorMessage = errorMessage;
			errorMessageBlock.Text = errorMessage;
		}

		public ErrorDialogView()
		{
			if (Design.IsDesignMode == false)
				throw new InvalidOperationException("Enable to use parameterless constructor in non design mode");

			InitializeComponent();

			errorMessage = "Example error message\n" + new string('c', 10000);
			errorMessageBlock.Text = errorMessage;
		}


		private void OnActivated(object? sender, EventArgs e)
		{
			copyButton.Click += async (_, _) =>
			{
				var task = Application.Current?.Clipboard?.SetTextAsync(errorMessage);
				if (task is not null) await task;
			};

			closeButton.Click += (_, _) => Close();
		}


		public static async ValueTask ShowAsync(string errorMessage, Window owner)
		{
			await new ErrorDialogView(errorMessage).ShowDialog(owner);
		}
	}
}

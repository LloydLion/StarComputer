using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Localization;

namespace StarComputer.UI.Avalonia
{
	public partial class ErrorDialogView : Window
	{
		private static LocalizationModel? localization = null;


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
			if (localization is null)
				throw new InvalidOperationException("Localize error dialog view before use");

			await new ErrorDialogView(errorMessage) { DataContext = localization }.ShowDialog(owner);
		}

		public static void LocalizeWith(IStringLocalizer<ErrorDialogView> localizer)
		{
			localization = new LocalizationModel(localizer);
		}


		private class LocalizationModel
		{
			private IStringLocalizer<ErrorDialogView> localizer;


			public LocalizationModel(IStringLocalizer<ErrorDialogView> localizer)
			{
				this.localizer = localizer;
			}


			public string WindowTitle => localizer[nameof(WindowTitle)];

			public string HeaderLabel => localizer[nameof(HeaderLabel)];

			public string ErrorMessageLabel => localizer[nameof(ErrorMessageLabel)];

			public string CopyToClipboardButton => localizer[nameof(CopyToClipboardButton)];

			public string ContinueButton => localizer[nameof(ContinueButton)];
		}
	}
}

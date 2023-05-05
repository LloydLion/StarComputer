using StarComputer.ApplicationUtils.Localization;

namespace StarComputer.UI.Avalonia.Translations
{
	internal class ErrorDialogViewTranslation : SmartStarComputerLocalizationProvider<ErrorDialogView>
	{
		public ErrorDialogViewTranslation()
		{
			AddLocale(new(""), adder =>
			{
				adder.AddTranslation("WindowTitle", "Error");
				adder.AddTranslation("HeaderLabel", "Operation finished with fatal error!");
				adder.AddTranslation("ErrorMessageLabel", "Error message:");
				adder.AddTranslation("CopyToClipboardButton", "Copy to clipboard");
				adder.AddTranslation("ContinueButton", "Continue");
			});

			AddLocale(new("ru"), adder =>
			{
				adder.AddTranslation("WindowTitle", "Ошибка");
				adder.AddTranslation("HeaderLabel", "Операция заверилась с фатальной ошибкой!");
				adder.AddTranslation("ErrorMessageLabel", "Текст ошибки:");
				adder.AddTranslation("CopyToClipboardButton", "Скопировать в буфер обмена");
				adder.AddTranslation("ContinueButton", "Продолжить");
			});
		}
	}
}

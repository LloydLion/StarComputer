using StarComputer.ApplicationUtils.Localization;

namespace StarComputer.Client.UI.Avalonia.Translations
{
	internal class ConnectionDialogViewTranslation : SmartStarComputerLocalizationProvider<ConnectionDialogView>
	{
		public ConnectionDialogViewTranslation()
		{
			AddLocale(new(""), adder =>
			{
				adder.AddTranslation("Title", "Connection");
				adder.AddTranslation("EndpointTextboxWatermark", "Endpoint");
				adder.AddTranslation("ServerPasswordTextboxWatermark", "Server password");
				adder.AddTranslation("LoginTextboxWatermark", "Login");
				adder.AddTranslation("ConnectButton", "Connect");
			});

			AddLocale(new("ru"), adder =>
			{
				adder.AddTranslation("Title", "Подключение");
				adder.AddTranslation("EndpointTextboxWatermark", "Адрес");
				adder.AddTranslation("ServerPasswordTextboxWatermark", "Пароль сервера");
				adder.AddTranslation("LoginTextboxWatermark", "Логин");
				adder.AddTranslation("ConnectButton", "Подключиться");
			});
		}
	}
}

using StarComputer.ApplicationUtils.Localization;

namespace StarComputer.Client.UI.Avalonia.Translations
{
	internal class ClientConnectionMenuViewTranslation : SmartStarComputerLocalizationProvider<ClientConnectionMenuView>
	{
		public ClientConnectionMenuViewTranslation()
		{
			AddLocale(new(""), adder =>
			{
				adder.AddTranslation("ConnectionMenuHeader", "Connection");
				adder.AddTranslation("CloseConnectionMenuHeader", "Close connection");
				adder.AddTranslation("OpenNewConnectionMenuHeader", "Open new connection");
			});

			AddLocale(new("ru"), adder =>
			{
				adder.AddTranslation("ConnectionMenuHeader", "Подключение");
				adder.AddTranslation("CloseConnectionMenuHeader", "Закрыть соединение");
				adder.AddTranslation("OpenNewConnectionMenuHeader", "Открыть новое соединение");
			});
		}
	}
}

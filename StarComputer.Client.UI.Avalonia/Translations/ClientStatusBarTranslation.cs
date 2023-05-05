using StarComputer.ApplicationUtils.Localization;
using System.Globalization;

namespace StarComputer.Client.UI.Avalonia.Translations
{
	internal class ClientStatusBarTranslation : SmartStarComputerLocalizationProvider<ClientStatusBarView>
	{
		public ClientStatusBarTranslation()
		{
			AddLocale(new(""), add =>
			{
				add.AddTranslation("ConnectedToLabel", "Connected to");
				add.AddTranslation("NoConnectionLabel", "No connection");
				add.AddTranslation("UsingInterfaceLabel", "Using {0} interface"); //{0} - Interface
				add.AddTranslation("LoggedAsLabel", "Logged as");
			});

			AddLocale(new("ru"), add =>
			{
				add.AddTranslation("ConnectedToLabel", "Подключён к");
				add.AddTranslation("NoConnectionLabel", "Нет подключения");
				add.AddTranslation("UsingInterfaceLabel", "Используется интерфейс {0}"); //{0} - Interface
				add.AddTranslation("LoggedAsLabel", "Вход выполнен за");
			});
		}
	}
}

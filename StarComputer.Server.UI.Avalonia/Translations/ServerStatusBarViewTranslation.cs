using StarComputer.ApplicationUtils.Localization;

namespace StarComputer.Server.UI.Avalonia.Translations
{
	internal class ServerStatusBarViewTranslation : SmartStarComputerLocalizationProvider<ServerStatusBarView>
	{
		public ServerStatusBarViewTranslation()
		{
			AddLocale(new(""), adder =>
			{
				adder.AddTranslation("ServerClosedLabel", "Server closed");
				adder.AddTranslation("ServerListeningOn", "Listening on {0}"); //{0} - Interface
			});

			AddLocale(new("ru"), adder =>
			{
				adder.AddTranslation("ServerClosedLabel", "Сервер закрыт");
				adder.AddTranslation("ServerListeningOn", "Сервер открыт на {0}"); //{0} - Interface
			});
		}
	}
}

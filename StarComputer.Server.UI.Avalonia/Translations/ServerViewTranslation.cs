using StarComputer.ApplicationUtils.Localization;

namespace StarComputer.Server.UI.Avalonia.Translations
{
	internal class ServerViewTranslation : SmartStarComputerLocalizationProvider<ServerView>
	{
		public ServerViewTranslation()
		{
			AddLocale(new(""), adder =>
			{
				adder.AddTranslation("WindowMenuItemHeader", "Window");
				adder.AddTranslation("OpenServerStatusControlMenuItemHeader", "Server status control");
			});

			AddLocale(new("ru"), adder =>
			{
				adder.AddTranslation("WindowMenuItemHeader", "Окно");
				adder.AddTranslation("OpenServerStatusControlMenuItemHeader", "Контроль статуса сервера");
			});
		}
	}
}

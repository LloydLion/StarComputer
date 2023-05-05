using StarComputer.ApplicationUtils.Localization;

namespace StarComputer.Server.UI.Avalonia.Translations
{
	internal class ServerControlViewTranslation : SmartStarComputerLocalizationProvider<ServerControlView>
	{
		public ServerControlViewTranslation()
		{
			AddLocale(new(""), adder =>
			{
				adder.AddTranslation("StartListeningButton", "Start listening");
			});

			AddLocale(new("ru"), adder =>
			{
				adder.AddTranslation("StartListeningButton", "Начать прослушивание");
			});
		}
	}
}

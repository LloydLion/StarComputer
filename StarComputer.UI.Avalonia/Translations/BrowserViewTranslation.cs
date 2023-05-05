using StarComputer.ApplicationUtils.Localization;

namespace StarComputer.UI.Avalonia.Translations
{
	internal class BrowserViewTranslation : SmartStarComputerLocalizationProvider<BrowserView>
	{
		public BrowserViewTranslation()
		{
			AddLocale(new(""), adder =>
			{
				adder.AddTranslation("CloseMenuItemHeader", "Close");
				adder.AddTranslation("OpenInRightSidebarMenuItemHeader", "Open in right sidebar");
				adder.AddTranslation("OpenInLeftSidebarMenuItemHeader", "Open in left sidebar");
				adder.AddTranslation("CombineMenuItemHeader", "Combine");
			});

			AddLocale(new("ru"), adder =>
			{
				adder.AddTranslation("CloseMenuItemHeader", "Закрыть");
				adder.AddTranslation("OpenInRightSidebarMenuItemHeader", "Прикрепить справа");
				adder.AddTranslation("OpenInLeftSidebarMenuItemHeader", "Прикрепить слева");
				adder.AddTranslation("CombineMenuItemHeader", "Комбинировать");
			});
		}
	}
}

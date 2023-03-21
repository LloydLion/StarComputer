using Avalonia.Controls;
using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.UI.Avalonia
{
	public partial class BrowserView : UserControl
	{
		public const string JSContextFieldName = "context";


		private Decorator? activeDecorator;
		private readonly Dictionary<PluginAvaloniaBrowser, Decorator> browsers = new();


		private BrowserViewModel Context => (BrowserViewModel)DataContext!;


		public BrowserView()
		{
			InitializeComponent();

			if (Design.IsDesignMode == false)
			{
				Initialized += OnBrowserViewInitialized;
				Background = null;
			}
		}


		private void OnBrowserViewInitialized(object? sender, EventArgs e)
		{
			Context.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(BrowserViewModel.ActivePlugin))
				{
					if (activeDecorator is not null)
						activeDecorator.IsVisible = false;

					if (Context.ActivePlugin is not null)
					{
						activeDecorator = browsers[Context.BrowsersToVisualize[Context.ActivePlugin.GetDomain()]];
						activeDecorator.IsVisible = true;
					}
				}
			};


			foreach (var item in Context.BrowsersToVisualize)
				CreateDecoratorFor(item);
		}


		private void CreateDecoratorFor(PluginAvaloniaBrowser browser)
		{
			if (browsers.ContainsKey(browser))
				return;

			var decorator = new Decorator();
			browsers.Add(browser, decorator);

			frame.Children.Add(decorator);
			browser.UseDecorator(decorator);

			decorator.IsVisible = false;
		}
	}
}

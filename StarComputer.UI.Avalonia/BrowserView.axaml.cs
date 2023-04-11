using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using JetBrains.Annotations;
using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.UI.Avalonia
{
	public partial class BrowserView : UserControl
	{
		public const string JSContextFieldName = "context";


		private Decorator? activeDecorator;
		private readonly Dictionary<PluginAvaloniaBrowser, Decorator> browsers = new();
		private readonly Dictionary<Decorator, PluginAvaloniaBrowser> browsersBackward = new();


		private BrowserViewModel Context => (BrowserViewModel)DataContext!;


		public BrowserView()
		{
			InitializeComponent();

			if (Design.IsDesignMode == false)
			{
				Initialized += OnBrowserViewInitialized;
#if DEBUG
				var showDevToolsBnt = new Button()
				{ Content = "Open developer tools", ZIndex = 10 };

				showDevToolsBnt.SetValue(Grid.RowProperty, 1);

				showDevToolsBnt.Click += OnShowDevTools;

				frame.RowDefinitions.Add(new RowDefinition());
				frame.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
				frame.Children.Add(showDevToolsBnt);
#endif
				Background = null;
			}
		}

		private void OnShowDevTools(object? sender, RoutedEventArgs e)
		{
			if (activeDecorator is not null)
				browsersBackward[activeDecorator].SetDevtoolsVisibility(true);
		}

		private void OnBrowserViewInitialized(object? sender, EventArgs e)
		{
			Context.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(BrowserViewModel.ActivePlugin))
				{
					if (activeDecorator is not null)
					{
						activeDecorator.IsVisible = false;
						browsersBackward[activeDecorator].SetDevtoolsVisibility(false);
					}

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
			browsersBackward.Add(decorator, browser);

			frame.Children.Add(decorator);
			browser.UseDecorator(decorator);

			decorator.IsVisible = false;
		}
	}
}

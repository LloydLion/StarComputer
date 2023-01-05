using Avalonia.Controls;

namespace StarComputer.Client.UI.Avalonia
{
	public partial class BrowserView : UserControl
	{
		public const string JSContextFieldName = "context";


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


		private void OnBrowserViewInitialized(object? sender, System.EventArgs e)
		{
			Context.PropertyChanged += (server, e) =>
			{
				if (e.PropertyName == nameof(BrowserViewModel.JSContext))
				{
					if (Context.JSContext is not null)
						browser.RegisterJavascriptObject(Context.JSContext, JSContextFieldName);
					else browser.UnregisterJavascriptObject(JSContextFieldName);
				}

				if (e.PropertyName == nameof(BrowserViewModel.Address) && Context.Address is not null)
					browser.Address = Context.Address;
			};

			if (Context.JSContext is not null) browser.RegisterJavascriptObject(Context.JSContext, JSContextFieldName);
			if (Context.Address is not null) browser.Address = Context.Address;
		}
	}
}

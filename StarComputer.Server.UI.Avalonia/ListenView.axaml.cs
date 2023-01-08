using Avalonia.Controls;

namespace StarComputer.Server.UI.Avalonia
{
	public partial class ListenView : UserControl
	{
		private ListenViewModel Context => (ListenViewModel)DataContext!;


		public ListenView()
		{
			InitializeComponent();

			if (Design.IsDesignMode == false)
				Initialized += OnListenViewInitialized;
		}


		private void OnListenViewInitialized(object? sender, System.EventArgs e)
		{
			listenButton.Click += async (_, e) =>
			{
				await Context.ListenAsync();
			};
		}
	}
}

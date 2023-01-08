using Avalonia.Controls;

namespace StarComputer.Server.UI.Avalonia
{
	public partial class MainWindow : Window
	{
		private MainWindowViewModel Context => (MainWindowViewModel)DataContext!;


		public MainWindow()
		{
			InitializeComponent();

			Closed += OnMainWindowClosed;
		}


		private void OnMainWindowClosed(object? sender, System.EventArgs e)
		{
			Context.Close();
		}

		public void Initialize(MainWindowViewModel viewModel)
		{
			DataContext = viewModel;
		}
	}
}
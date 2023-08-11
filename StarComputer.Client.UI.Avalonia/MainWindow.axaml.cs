using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Linq;

namespace StarComputer.Client.UI.Avalonia
{
	public partial class MainWindow : Window
	{
		private MainWindowViewModel Context => (MainWindowViewModel)DataContext!;


		public MainWindow()
		{
			InitializeComponent();

			Closed += OnMainWindowClosed;
		}


		private void OnMainWindowClosed(object? sender, EventArgs e)
		{
			Context.Close();
		}

		public void Initialize(MainWindowViewModel viewModel)
		{
			DataContext = viewModel;
		}
	}
}
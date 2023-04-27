using Avalonia.Controls;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public partial class ConnectionDialogView : Window
	{
		private ConnectionDialogViewModel Context => (ConnectionDialogViewModel)DataContext!;


		public ConnectionDialogView()
		{
			InitializeComponent();
			if (Design.IsDesignMode == false)
				Activated += OnActivated;
		}


		private void OnActivated(object? sender, EventArgs e)
		{
			connectButton.Click += (_, _) =>
			{
				Close(Context.FormConnectionConfiguration());
			};
		}
	}
}

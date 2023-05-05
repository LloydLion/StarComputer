using Avalonia.Controls;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public partial class ClientView : UserControl
	{
		private ClientViewModel Context => (ClientViewModel)DataContext!;


		public ClientView()
		{
			InitializeComponent();
			if (Design.IsDesignMode == false)
				Initialized += OnInitialized;
		}


		private void OnInitialized(object? sender, EventArgs e)
		{
			var connectionMenu = new MenuItem();
			new ClientConnectionMenuView().Attach(connectionMenu, Context.ConnectionMenu);

			menu.Items = new MenuItem[]
			{
				connectionMenu
			};
		}
	}
}

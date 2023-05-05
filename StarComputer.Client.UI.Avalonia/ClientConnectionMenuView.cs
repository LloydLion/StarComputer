using Avalonia.Controls;

namespace StarComputer.Client.UI.Avalonia
{
	public class ClientConnectionMenuView
	{
		public void Attach(MenuItem item, ClientConnectionMenuViewModel context)
		{
			item.Header = context.Localization.ConnectionMenuHeader;

			var closeConnectionMenuItem = new MenuItem() { Header = context.Localization.CloseConnectionMenuHeader };
			var openNewConnectionMenuItem = new MenuItem() { Header = context.Localization.OpenNewConnectionMenuHeader };

			item.Items = new[]
			{
				openNewConnectionMenuItem,
				closeConnectionMenuItem
			};


			context.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(ClientConnectionMenuViewModel.IsConnected))
				{
					closeConnectionMenuItem.IsEnabled = context.IsConnected;
				}
			};

			closeConnectionMenuItem.Click += async (_, _) => await context.DisconnectAsync();

			openNewConnectionMenuItem.Click += async (_, _) => await context.TryConnectToNewServerAsync();
		}
	}
}

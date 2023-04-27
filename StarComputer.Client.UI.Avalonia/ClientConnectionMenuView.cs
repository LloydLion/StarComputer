using Avalonia.Controls;
using Avalonia.LogicalTree;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public static class ClientConnectionMenuView
	{
		public static void Attach(MenuItem item, ClientConnectionMenuViewModel context)
		{
			item.Header = "Connection";

			var closeConnectionMenuItem = new MenuItem() { Header = "Close connection" };
			var openNewConnectionMenuItem = new MenuItem() { Header = "Open new connection" };

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

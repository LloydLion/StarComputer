using Avalonia;
using Avalonia.Controls;
using System;

namespace StarComputer.Server.UI.Avalonia
{
	public partial class ServerView : UserControl
	{
		public ServerView()
		{
			InitializeComponent();
			if (Design.IsDesignMode == false)
				Initialized += OnInitialized;
		}


		private void OnInitialized(object? sender, EventArgs e)
		{
			serverControlView.AttachOnClosed(() =>
			{
				serverStatusControlMenuItem.IsEnabled = true;

				serverControlView.IsVisible = false;
			});

			serverStatusControlMenuItem.Click += (_, _) =>
			{
				serverStatusControlMenuItem.IsEnabled = false;

				serverControlView.IsVisible = true;
			};
		}
	}
}

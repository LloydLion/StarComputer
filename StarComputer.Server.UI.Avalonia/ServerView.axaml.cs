using Avalonia;
using Avalonia.Controls;
using StarComputer.ApplicationUtils.Localization;
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
			else
			{
				DataContext = new { Localization = new ServerViewModel.LocalizationModel(DesignLocalizer.Instance) };
			}
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

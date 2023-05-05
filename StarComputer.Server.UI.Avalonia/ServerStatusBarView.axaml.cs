using Avalonia.Controls;
using Avalonia.Media;
using StarComputer.ApplicationUtils.Localization;
using System;

namespace StarComputer.Server.UI.Avalonia
{
	public partial class ServerStatusBarView : UserControl
	{
		private ServerStatusBarViewModel Context => (ServerStatusBarViewModel)DataContext!;


		public ServerStatusBarView()
		{
			InitializeComponent();
			if (Design.IsDesignMode == false)
				Initialized += OnInitialized;
			else
			{
				DataContext = new
				{
					Localization = new ServerStatusBarViewModel.LocalizationModel(DesignLocalizer.Instance),
					IsNotListening = true
				};
			}
		}


		private void OnInitialized(object? sender, EventArgs e)
		{
			blub.Fill = new SolidColorBrush(Color.Parse("Blue"));
			Context.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(ServerStatusBarViewModel.IsListening))
				{
					blub.Fill = new SolidColorBrush(Color.Parse(Context.IsListening ? "LightGreen" : "Blue"));
				}
			};

			listeningLabel.Content = string.Format(Context.Localization.ServerListeningOn, Context.Configuration.Interface);
		}
	}
}

using Avalonia.Controls;
using Avalonia.Media;
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
		}
	}
}

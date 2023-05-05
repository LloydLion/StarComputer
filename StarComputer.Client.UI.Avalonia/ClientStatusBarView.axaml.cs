using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace StarComputer.Client.UI.Avalonia
{
	public partial class ClientStatusBarView : UserControl
	{
		private ClientStatusBarViewModel Context => (ClientStatusBarViewModel)DataContext!;


		public ClientStatusBarView()
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
				if (e.PropertyName == nameof(ClientStatusBarViewModel.IsConnected))
				{
					blub.Fill = new SolidColorBrush(Color.Parse(Context.IsConnected ? "LightGreen" : "Blue"));
				}
			};

			interfaceLabel.Content = string.Format(Context.Localization.UsingInterfaceLabel, Context.Configuration.Interface);
		}
	}
}

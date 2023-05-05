using Avalonia.Controls;
using StarComputer.ApplicationUtils.Localization;
using System;

namespace StarComputer.Server.UI.Avalonia
{
	public partial class ServerControlView : UserControl
	{
		private Action? onClosedAction;


		private ServerControlViewModel Context => (ServerControlViewModel)DataContext!;


		public ServerControlView()
		{
			InitializeComponent();

			if (Design.IsDesignMode == false)
				Initialized += OnInitialized;
			else
			{
				DataContext = new
				{
					Localization = new ServerControlViewModel.LocalizationModel(DesignLocalizer.Instance),
					IsListening = true,
					Clients = new[]
					{
						new ServerControlViewModel.ServerClientUIDTO("Dummy", "128.0.1.3:3412"),
						new ServerControlViewModel.ServerClientUIDTO("Dummy2", "128.2.4.3:3412")
					}
				};
			}
		}


		private void OnInitialized(object? sender, EventArgs e)
		{
			listenButton.Click += async (_, _) =>
			{
				await Context.ListenAsync();
			};

			closeMenuButton.Click += (_, _) =>
			{
				onClosedAction?.Invoke();
			};
		}

		public void AttachOnClosed(Action onClosedAction)
		{
			this.onClosedAction = onClosedAction;
		}
	}
}

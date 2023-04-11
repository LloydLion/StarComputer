using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Threading;
using StarComputer.UI.Avalonia;
using System.Threading.Tasks;

namespace StarComputer.Server.UI.Avalonia
{
	public partial class App : Application
	{
		private InitializationData? initialization;


		public void Setup(IServiceProvider services, Action<dynamic> callback, dynamic parameter)
		{
			initialization = new(services, callback, parameter);
		}

		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (Design.IsDesignMode)
			{
				DataTemplates.Add(new ViewLocator());

				if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
				{
					var window = new MainWindow();
					desktop.MainWindow = window;
				}
			}
			else
			{
				DataTemplates.Add(new ViewLocator());

				if (initialization is null)
					throw new InvalidOperationException("Setup application before framework initialization completed");

				initialization.Callback(initialization.Parameter);

				if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
				{
					var window = new MainWindow();
					window.Initialize(new MainWindowViewModel(initialization.Services));

					desktop.MainWindow = window;
				}
			}

			base.OnFrameworkInitializationCompleted();
		}


		private record InitializationData(IServiceProvider Services, Action<dynamic> Callback, dynamic Parameter);
	}
}

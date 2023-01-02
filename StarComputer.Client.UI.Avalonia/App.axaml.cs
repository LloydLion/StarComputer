using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using StarComputer.Client.Abstractions;
using System.Threading;
using Microsoft.Extensions.Options;

namespace StarComputer.Client.UI.Avalonia
{
	public partial class App : Application
	{
		private IServiceProvider? services;


		public IServiceProvider Services => services!;


		public void Setup(IServiceProvider services)
		{
			this.services = services; 
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
					var window = new MainWindow { Content = new ClientView() };
					desktop.MainWindow = window;
				}
			}
			else
			{
				DataTemplates.Add(new ViewLocator());

				while (services is null)
					Thread.Sleep(10);

				if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
				{
					var window = new MainWindow();
					window.Initialize(new MainWindowViewModel(
						services.GetRequiredService<IClient>(),
						services.GetRequiredService<AvaloniaBasedConsoleUIContext>(),
						services.GetRequiredService<IOptions<ClientViewModel.Options>>()));
					desktop.MainWindow = window;
				}
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}
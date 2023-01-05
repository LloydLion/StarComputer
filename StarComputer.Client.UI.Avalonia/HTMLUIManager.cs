using StarComputer.Common.Abstractions.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace StarComputer.Client.UI.Avalonia
{
	public class HTMLUIManager : IUIContextFactory<HTMLUIContext>
	{
		private readonly Dictionary<IPlugin, HTMLUIContext> contexts = new();
		private IPlugin? activePlugin;


		public IPlugin? ActivePlugin
		{
			get => activePlugin;
			private set
			{
				activePlugin = value;

				if (ActiveContext is not null)
					ActiveContext.GetView().PropertyChanged -= OnViewPropertyChanged;

				if (activePlugin is not null)
				{
					ActiveContext = contexts[activePlugin];
					ActiveContext.GetView().PropertyChanged += OnViewPropertyChanged;
				}
				else ActiveContext = null;

				ActiveContextChanged?.Invoke(ContextChangingType.ActivePluginChanged);
			}	
		}

		public HTMLUIContext? ActiveContext { get; private set; }


		public event Action<ContextChangingType>? ActiveContextChanged;


		public HTMLUIContext CreateContext(IPlugin plugin)
		{
			if (contexts.ContainsKey(plugin)) return contexts[plugin];
			else
			{
				var context = new HTMLUIContext(this, plugin);
				contexts.Add(plugin, context);

				return context;
			}
		}

		public void SwitchPlugin(IPlugin? plugin)
		{
			ActivePlugin = plugin;
		}

		private void OnViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(HTMLUIContext.HTMLView.Address))
				ActiveContextChanged?.Invoke(ContextChangingType.AddressChanged);

			if (e.PropertyName == nameof(HTMLUIContext.HTMLView.JSContext))
				ActiveContextChanged?.Invoke(ContextChangingType.JSContextChanged);
		}


		public enum ContextChangingType
		{
			ActivePluginChanged,
			AddressChanged,
			JSContextChanged
		}
	}
}

using Avalonia.Controls;
using System;

namespace StarComputer.UI.Avalonia
{
	public partial class PluginSelectorView : UserControl
	{
		private PluginSelectorViewModel Context => (PluginSelectorViewModel)DataContext!;


		public PluginSelectorView()
		{
			InitializeComponent();

			if (Design.IsDesignMode == false)
				Initialized += OnViewInitialized;
		}


		private void OnViewInitialized(object? sender, EventArgs e)
		{
			pluginSelector.Items = Context.Plugins;
			pluginSelector.SelectionChanged += (_, e) =>
			{
				if (e.AddedItems.Count == 1)
				{
					var plugin = e.AddedItems[0];
					Context.SwitchPlugin(plugin);
				}
			};
		}
	}
}

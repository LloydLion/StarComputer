using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;
using System;
using System.Collections.Generic;

namespace StarComputer.UI.Avalonia
{
	public class HTMLUIManager : IUIContextFactory<HTMLUIContext>
	{
		private readonly Dictionary<IPlugin, HTMLUIContext> contexts = new();
		private readonly IResourcesCatalog resources;
		private IPlugin? activePlugin;
		private JavaScriptExecutor? executor;


		public delegate dynamic? JavaScriptExecutor(IPlugin caller, string functionName, object[] arguments);


		public IPlugin? ActivePlugin
		{
			get => activePlugin;
			private set
			{
				activePlugin = value;

				if (activePlugin is not null)
					ActiveContext = contexts[activePlugin];
				else ActiveContext = null;

				ContextChanged?.Invoke(ContextChangingType.ActivePluginChanged, ActiveContext);
			}	
		}

		public HTMLUIContext? ActiveContext { get; private set; }

		public IReadOnlyDictionary<IPlugin, HTMLUIContext> Contexts => contexts;


		public HTMLUIManager(IResourcesCatalog resources)
		{
			this.resources = resources;
		}


		public event Action<ContextChangingType, HTMLUIContext?>? ContextChanged;


		public HTMLUIContext CreateContext(IPlugin plugin)
		{
			if (contexts.ContainsKey(plugin)) return contexts[plugin];
			else
			{
				var context = new HTMLUIContext(this, plugin, resources.GetResourcesFor(plugin));

				context.JSContextChanged += OnJSContextChanged;
				context.NewPageLoaded += OnNewPageLoaded;

				contexts.Add(plugin, context);

				return context;
			}
		}

		public HTMLUIContext GetContext(IPlugin plugin) => contexts[plugin];

		public void SwitchPlugin(IPlugin? plugin)
		{
			ActivePlugin = plugin;
		}

		public void SetJavaScriptExecutor(JavaScriptExecutor executor)
		{
			this.executor = executor;
		}

		public dynamic? ExecuteJavaScript(IPlugin caller, string functionName, params object[] arguments)
		{
			if (executor is null)
				throw new NullReferenceException("JavaScript executor was null");
			return executor(caller, functionName, arguments);
		}

		private void OnJSContextChanged(object? sender, EventArgs _) =>
			ContextChanged?.Invoke(ContextChangingType.JSContextChanged, (HTMLUIContext?)sender);

		private void OnNewPageLoaded(object? sender, EventArgs _) =>
			ContextChanged?.Invoke(ContextChangingType.AddressChanged, (HTMLUIContext?)sender);

		public void InitializePostUI()
		{
			foreach (var context in contexts)
				context.Value.InitializePostUI();
		}

		public enum ContextChangingType
		{
			ActivePluginChanged,
			AddressChanged,
			JSContextChanged
		}
	}
}

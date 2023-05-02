using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Newtonsoft.Json;
using StarComputer.Common.Abstractions.Threading;
using System.ComponentModel;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;
using Xilium.CefGlue.Common.Handlers;

namespace StarComputer.UI.Avalonia
{
	public class PluginAvaloniaBrowser : INotifyPropertyChanged
	{
		public const string JSContextFieldName = "context";


		private readonly AvaloniaCefBrowser browser;
		private readonly IThreadDispatcher<Action> mainThreadDispatcher;
		private string? currentUrl;
		private bool isInitialized = false;
		private Decorator? currentDecorator;


		public event PropertyChangedEventHandler? PropertyChanged;

		public event EventHandler? BrowserReloaded;


		public string? CurrentUrl { get => currentUrl; private set => SetAndNotify(ref currentUrl, value); }

		public bool IsInitialized { get => isInitialized; private set => SetAndNotify(ref isInitialized, value); }


		public PluginAvaloniaBrowser(IThreadDispatcher<Action> mainThreadDispatcher, Action initializeCallback)
		{
			this.mainThreadDispatcher = mainThreadDispatcher;

			if (Dispatcher.UIThread.CheckAccess())
				browser = new();
			else browser = Dispatcher.UIThread.InvokeAsync(() => new AvaloniaCefBrowser(), DispatcherPriority.Send).Result;

			browser.DownloadHandler = new CustomDownloadHandler();
			browser.ContextMenuHandler = new CustomContextMenuHandler();

			browser.AddressChanged += (sender, e) =>
			{
				CurrentUrl = e;
				BrowserReloaded?.Invoke(this, EventArgs.Empty);
			};

			browser.BrowserInitialized += () =>
			{
				IsInitialized = true;
				initializeCallback();
			};
		}


		public void UseDecorator(Decorator decorator)
		{
			if (currentDecorator is not null)
				currentDecorator.Child = null;

			decorator.Child = null;
			decorator.Child = browser;

			currentDecorator = decorator;
		}

		public async Task NavigateAsync(string? url, bool forceReload = false)
		{
			if (browser.Address != url || forceReload)
			{
				var loadEvent = new TaskCompletionSource();

				browser.LoadEnd += handle;

				browser.Address = url;

				await loadEvent.Task;

				browser.LoadEnd -= handle;



				void handle(object sender, LoadEndEventArgs args)
				{
					loadEvent.SetResult();
				}
			}
		}

		public void ForceReload()
		{
			browser.Address = CurrentUrl;
		}

		public dynamic? ExecuteJavaScript(string functionName, params object?[] arguments)
		{
			if (IsInitialized == false)
				throw new InvalidOperationException("Enable to execute JS code before initialization");

			if (Dispatcher.UIThread.CheckAccess())
				return execute();
			else return Dispatcher.UIThread.InvokeAsync(execute, DispatcherPriority.Send).Result;

			dynamic execute()
			{
				var jsonArgs = arguments.Select(s => JsonConvert.SerializeObject(s)).ToArray();
				var code = $"{functionName}({string.Join(", ", jsonArgs)});";

				return browser.EvaluateJavaScript<ExpandoObject>(code).Result;
			}
		}

		public void SetJavaScriptContext(object context)
		{
			browser.RegisterJavascriptObject(context, JSContextFieldName, callNativeMethod);


			Task<object> callNativeMethod(Func<object> nativeMethod)
			{
				var tcs = new TaskCompletionSource<object>();

				mainThreadDispatcher.DispatchTask(() =>
				{
					try
					{
						var obj = nativeMethod.Invoke();
						tcs.SetResult(obj);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
						throw;
					}
				});

				return tcs.Task;
			}
		}

		public void SetDevtoolsVisibility(bool status)
		{
			if (status)
				browser.ShowDeveloperTools();
			else browser.CloseDeveloperTools();
		}

		private void SetAndNotify<T>(ref T variable, T newValue, [CallerMemberName] string propertyName = "Auto generated")
		{
			if (Equals(variable, newValue) == false)
			{
				variable = newValue;
				PropertyChanged?.Invoke(this, new(propertyName));
			}
		}


		private class CustomDownloadHandler : DownloadHandler
		{
			protected override bool CanDownload(CefBrowser browser, string url, string requestMethod)
			{
				return true;
			}

			protected override void OnBeforeDownload(CefBrowser browser, CefDownloadItem downloadItem, string suggestedName, CefBeforeDownloadCallback callback)
			{
				callback.Continue(suggestedName, showDialog: true);
			}

			protected override void OnDownloadUpdated(CefBrowser browser, CefDownloadItem downloadItem, CefDownloadItemCallback callback)
			{
				callback.Resume();
			}
		}

		private class CustomContextMenuHandler : ContextMenuHandler
		{
			protected override void OnBeforeContextMenu(CefBrowser browser, CefFrame frame, CefContextMenuParams state, CefMenuModel model)
			{
				if (state.ContextMenuType == CefContextMenuTypeFlags.Frame || state.ContextMenuType == CefContextMenuTypeFlags.Page)
					model.Clear();
			}
		}
	}
}

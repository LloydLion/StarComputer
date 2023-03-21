using Avalonia.Controls;
using Avalonia.Threading;
using Newtonsoft.Json;
using StarComputer.Common.Abstractions.Threading;
using System.ComponentModel;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Xilium.CefGlue.Avalonia;

namespace StarComputer.UI.Avalonia
{
	public class PluginAvaloniaBrowser : INotifyPropertyChanged
	{
		public const string JSContextFieldName = "context";


		private readonly AvaloniaCefBrowser browser;
		private readonly IThreadDispatcher<Action> mainThreadDispatcher;
		private string? currentUrl;
		private bool isInitialized = false;


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
			decorator.Child = browser;
		}

		public void Navigate(string? url, bool forceReload = false)
		{
			if (browser.Address != url || forceReload)
				browser.Address = url;
		}

		public void ForceReload()
		{
			browser.Address = CurrentUrl;
		}

		public dynamic? ExecuteJavaScript(string functionName, params object[] arguments)
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


		private void SetAndNotify<T>(ref T variable, T newValue, [CallerMemberName] string propertyName = "Auto generated")
		{
			if (Equals(variable, newValue) == false)
			{
				variable = newValue;
				PropertyChanged?.Invoke(this, new(propertyName));
			}
		}
	}
}

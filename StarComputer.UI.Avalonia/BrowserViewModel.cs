using Avalonia.Threading;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Threading;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StarComputer.UI.Avalonia
{
	public class BrowserViewModel : ViewModelBase
	{
		private readonly HTMLUIManager manager;
		private readonly IThreadDispatcher<Action> mainThreadDispatcher;


		public BrowserViewModel(HTMLUIManager manager, IThreadDispatcher<Action> mainThreadDispatcher)
		{
			this.manager = manager;
			this.mainThreadDispatcher = mainThreadDispatcher;
			manager.ContextChanged += (a, b) => Dispatcher.UIThread.Post(() => ContextChanged?.Invoke(a, b), DispatcherPriority.Send);
		}


		public event Action<HTMLUIManager.ContextChangingType, HTMLUIContext?>? ContextChanged;


		public IReadOnlyDictionary<IPlugin, HTMLUIContext> Contexts => manager.Contexts;


		public void SetJavaScriptExecutor(HTMLUIManager.JavaScriptExecutor executor)
		{
			dynamic? wrap(IPlugin plugin, string functionName, object[] args)
			{
				var cell = new dynamic?[1];
				var cevent = new AutoResetEvent(false);

				Dispatcher.UIThread.Post(() =>
				{
					cell[0] = executor(plugin, functionName, args);
					cevent.Set();
				}, DispatcherPriority.Send);

				cevent.WaitOne();

				return cell[0];
			}

			manager.SetJavaScriptExecutor(wrap);
		}

		public Task<object> AsyncCallNativeMethod(Func<object> nativeMethod)
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
				}
			});

			return tcs.Task;
		}

		public void InitializePostUI()
		{
			mainThreadDispatcher.DispatchTask(() =>
			{
				manager.InitializePostUI();
			});
		}
	}
}

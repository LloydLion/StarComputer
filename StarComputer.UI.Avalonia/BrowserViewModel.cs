using Avalonia.Threading;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Threading;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reactive;
using System.Reflection;

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
			HTMLUIManager.JavaScriptExecutor wrap = (plugin, functionName, args) =>
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
			};

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


		//private class WrappedContext : DynamicObject
		//{
		//	private readonly object rawContext;
		//	private readonly BrowserViewModel owner;
		//	private readonly Dictionary<string, MethodInfo> methods = new();


		//	public WrappedContext(object rawContext, BrowserViewModel owner)
		//	{
		//		this.rawContext = rawContext;
		//		this.owner = owner;
		//		methods = rawContext.GetType().GetMethods().ToDictionary(s => s.Name);
		//	}


		//	public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
		//	{
		//		result = null;
		//		if (methods.ContainsKey(binder.Name) == false) return false;

		//		var method = methods[binder.Name];

		//		var ctsType = typeof(TaskCompletionSource<>).MakeGenericType(method.ReturnType == typeof(void) ? typeof(Unit) : method.ReturnType);
		//		var cts = Activator.CreateInstance(ctsType)!;

		//		owner.mainThreadDispatcher.DispatchTask(() =>
		//		{
		//			try
		//			{
		//				var result = method.Invoke(rawContext, args) ?? Unit.Default;
		//				ctsType.GetMethod(nameof(TaskCompletionSource.SetResult))!.Invoke(cts, new[] { result });
		//			}
		//			catch (Exception ex)
		//			{
		//				ctsType.GetMethod(nameof(TaskCompletionSource.SetException))!.Invoke(cts, new[] { ex });
		//			}
		//		});


		//		result = ctsType.GetProperty(nameof(TaskCompletionSource.Task))!.GetValue(cts);
		//		return true;
		//	}
		//}
	}
}

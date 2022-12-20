using StarComputer.Common.Abstractions.Threading;
using System.Collections.Concurrent;

namespace StarComputer.Common.Threading
{
	public class ThreadDispatcher<TTask> : IThreadDispatcher<TTask> where TTask : notnull
	{
		private readonly ConcurrentQueue<TTask> tasks = new();
		private readonly AutoResetEvent onNewTask = new(false);
		private readonly AutoResetEvent onClose = new(false);
		private readonly Thread targetThread;
		private readonly Action<TTask> taskExecutor;
		private WaitHandle[]? handles = null;


		public ThreadDispatcher(Thread targetThread, Action<TTask> taskExecutor)
		{
			this.targetThread = targetThread;
			this.taskExecutor = taskExecutor;
		}


		public void DispatchTask(TTask task)
		{
			tasks.Enqueue(task);
			onNewTask.Set();
		}

		public bool ExecuteTask()
		{
			if (tasks.TryDequeue(out var task))
			{
				taskExecutor(task);
				return true;
			}
			else return false;
		}

		public void Close()
		{
			onClose.Set();
		}

		public IEnumerable<TTask> GetQueue()
		{
			return tasks.ToArray();
		}

		public SynchronizationContext CraeteSynchronizationContext(Func<Action, TTask> packer)
		{
			return new DispatcherSynchronizationContext(this, packer);
		}

		public int WaitHandles(ReadOnlySpan<WaitHandle> handles, int timeout = -1)
		{
			if (this.handles is null || this.handles.Length != handles.Length + 2)
				this.handles = new WaitHandle[handles.Length + 2];

			this.handles[0] = onNewTask;
			this.handles[1] = onClose;
			handles.CopyTo(this.handles.AsSpan(2));

			var index = WaitHandle.WaitAny(this.handles, timeout);

			if (index == 0) return ThreadDispatcherStatic.NewTaskIndex;
			else if (index == 1) return ThreadDispatcherStatic.ClosedIndex;
			else if (index == WaitHandle.WaitTimeout) return ThreadDispatcherStatic.TimeoutIndex;
			else return index - 2;
		}


		private class DispatcherSynchronizationContext : SynchronizationContext
		{
			private readonly ThreadDispatcher<TTask> dispatcher;
			private readonly Func<Action, TTask> packer;


			public DispatcherSynchronizationContext(ThreadDispatcher<TTask> dispatcher, Func<Action, TTask> packer)
			{
				this.dispatcher = dispatcher;
				this.packer = packer;
			}


			public override SynchronizationContext CreateCopy()
			{
				return new DispatcherSynchronizationContext(dispatcher, packer);
			}

			public override void Post(SendOrPostCallback d, object? state)
			{
				var task = packer(() => d(state));
				dispatcher.DispatchTask(task);
			}

			public override void Send(SendOrPostCallback d, object? state)
			{
				if (dispatcher.targetThread.ManagedThreadId == Environment.CurrentManagedThreadId)
					d(state);
				else
				{
					var resetEvent = new AutoResetEvent(false);

					var task = packer(() =>
					{
						d(state);
						resetEvent.Set();
					});

					dispatcher.DispatchTask(task);

					resetEvent.WaitOne();
				}
			}
		}
	}
}

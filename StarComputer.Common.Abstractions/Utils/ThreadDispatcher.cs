using System.Collections.Concurrent;

namespace StarComputer.Common.Abstractions.Utils
{
	public class ThreadDispatcher<TTask> where TTask : notnull
	{
		private readonly ConcurrentQueue<TTask> tasks = new();
		private readonly WaitHandle?[] handles;
		private readonly AutoResetEvent onNewTask = new(false);
		private readonly AutoResetEvent onClose = new(false);
		private readonly Thread targetThread;
		private readonly Action<TTask> taskExecutor;


		public ThreadDispatcher(Thread targetThread, Action<TTask> taskExecutor)
		{
			handles = new WaitHandle[2];
			handles[0] = onNewTask;
			handles[1] = onClose;
			this.targetThread = targetThread;
			this.taskExecutor = taskExecutor;
		}

		public ThreadDispatcher(Thread targetThread, Action<TTask> taskExecutor, int otherWaits)
		{
			handles = new WaitHandle[otherWaits + 2];
			handles[0] = onNewTask;
			handles[1] = onClose;
			this.targetThread = targetThread;
			this.taskExecutor = taskExecutor;
		}


		public void DispatchTask(TTask task)
		{
			tasks.Enqueue(task);
			onNewTask.Set();
		}

		public int WaitHandlers(params WaitHandle[] parameters)
		{
			parameters.CopyTo(handles, 2);
#nullable disable
			return WaitHandle.WaitAny(handles) - 2;
#nullable restore
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

		public void ExecuteAllTasks()
		{
			while (tasks.TryDequeue(out var task))
			{
				taskExecutor(task);
			}
		}

		public void Close()
		{
			onClose.Set();
		}

		public ConcurrentQueue<TTask> GetQueueUnsafe()
		{
			return tasks;
		}

		public SynchronizationContext CraeteSynchronizationContext(Func<Action, TTask> packer)
		{
			return new DispatcherSynchronizationContext(this, packer);
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

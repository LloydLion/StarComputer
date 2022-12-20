namespace StarComputer.Common.Abstractions.Threading
{
	public interface IThreadDispatcher<TTask> where TTask : notnull
	{
		public void DispatchTask(TTask task);

		public bool ExecuteTask();

		public void Close();

		public SynchronizationContext CraeteSynchronizationContext(Func<Action, TTask> packer);

		public IEnumerable<TTask> GetQueue();

		public int WaitHandles(ReadOnlySpan<WaitHandle> handles, int timeout = -1);
	}

	public static class ThreadDispatcherStatic
	{
		public const int TimeoutIndex = -3;

		public const int NewTaskIndex = -2;

		public const int ClosedIndex = -1;
	}
}

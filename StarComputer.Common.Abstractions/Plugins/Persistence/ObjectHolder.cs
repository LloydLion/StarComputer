namespace StarComputer.Common.Abstractions.Plugins.Persistence
{
	public class ObjectHolder<TObject> : IDisposable where TObject : class
	{
		private bool isDisposed;
		private readonly TObject internalObject;
		private readonly Action saveCallback;


		public ObjectHolder(TObject internalObject, Action saveCallback)
		{
			this.internalObject = internalObject;
			this.saveCallback = saveCallback;
		}


		public TObject Object => isDisposed == false ? internalObject : throw new InvalidOperationException("Enable to get object after saving");


		public void Save() => Dispose();

		public void Dispose()
		{
			if (isDisposed)
				throw new InvalidOperationException("Enable to save object holder twice");
			isDisposed = true;

			GC.SuppressFinalize(this);

			saveCallback();
		}
	}
}

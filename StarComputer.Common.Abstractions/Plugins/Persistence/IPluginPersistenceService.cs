namespace StarComputer.Common.Abstractions.Plugins.Persistence
{
	public interface IPluginPersistenceService
	{
		public ValueTask SaveRawDataAsync(PersistenceAddress address, ReadOnlyMemory<byte> bytes);

		public ValueTask<ReadOnlyMemory<byte>> LoadRawDataAsync(PersistenceAddress address);

		public ObjectHolder<TObject> GetObject<TObject>(PersistenceAddress address) where TObject : class;

		public TObject ReadObject<TObject>(PersistenceAddress address) where TObject : class;
	}
}

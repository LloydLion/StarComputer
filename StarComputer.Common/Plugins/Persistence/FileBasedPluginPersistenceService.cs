using StarComputer.Common.Abstractions;
using StarComputer.Common.Abstractions.Plugins.Persistence;

namespace StarComputer.Common.Plugins.Persistence
{
	internal class FileBasedPluginPersistenceService : IPluginPersistenceService
	{
		private readonly string basePath;


		public FileBasedPluginPersistenceService(string basePath)
		{
			this.basePath = basePath;
		}


		public ObjectHolder<TObject> GetObject<TObject>(PersistenceAddress address) where TObject : class
		{
			var path = Path.Combine(basePath, address.Path);
			var rawObject = ReadObject<TObject>(address);
			return new ObjectHolder<TObject>(rawObject, () =>
			{
				if (File.Exists(path) == false)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new NullReferenceException());
					File.Create(path).Close();
				}
				File.WriteAllText(path, SerializationContext.Instance.Serialize(rawObject));
			});
		}

		public async ValueTask<ReadOnlyMemory<byte>> LoadRawDataAsync(PersistenceAddress address)
		{
			return await File.ReadAllBytesAsync(Path.Combine(basePath, address.Path));
		}

		public TObject ReadObject<TObject>(PersistenceAddress address) where TObject : class
		{
			var path = Path.Combine(basePath, address.Path);
			if (File.Exists(path) == false)
			{
				return Activator.CreateInstance<TObject>();
			}
			else
			{
				var rawObject = SerializationContext.Instance.Deserialize<TObject>(File.ReadAllText(path));
				return rawObject;
			}
		}

		public async ValueTask SaveRawDataAsync(PersistenceAddress address, ReadOnlyMemory<byte> bytes)
		{
			using var stream = File.OpenWrite(Path.Combine(basePath, address.Path));
			await stream.WriteAsync(bytes);
		}
	}
}

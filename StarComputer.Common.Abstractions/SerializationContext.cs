using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StarComputer.Common.Abstractions
{
	public abstract class SerializationContext
	{
		private static SerializationContext? instance;


		public static SerializationContext Instance
		{
			get
			{
				if (instance is null)
					Initialize(new JsonSerializationContext());

				return instance!;
			}
		}

		public static void Initialize(SerializationContext context)
		{
			if (instance is not null)
				throw new InvalidOperationException("Serialization context already initialized by " + instance.GetType().FullName);
			instance = context;
		}


		public abstract string Serialize(object? value);

		public abstract object? Deserialize(string value, Type objectType);

		public abstract object? SubDeserialize(object? mediateObject, Type objectType);

		public TObject Deserialize<TObject>(string value) where TObject : notnull
		{
			var valueObject = Deserialize(value, typeof(TObject));
			if (valueObject is null) return Activator.CreateInstance<TObject>();
			else return (TObject)valueObject;
		}

		public TObject SubDeserialize<TObject>(object mediateObject) where TObject : notnull
		{
			var valueObject = SubDeserialize(mediateObject, typeof(TObject));
			if (valueObject is null) return Activator.CreateInstance<TObject>();
			else return (TObject)valueObject;
		}


		private class JsonSerializationContext : SerializationContext
		{
			public override object? Deserialize(string value, Type objectType)
			{
				return JsonConvert.DeserializeObject(value, objectType);
			}

			public override string Serialize(object? value)
			{
				return JsonConvert.SerializeObject(value);
			}

			public override object? SubDeserialize(object? mediateObject, Type objectType)
			{
				if (mediateObject is null) return null;
				var token = (JToken)mediateObject;
				return token.ToObject(objectType);
			}
		}
	}
}

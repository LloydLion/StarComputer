using StarComputer.Common.Abstractions.Protocol.Bodies;

namespace StarComputer.Common.Protocol.Bodies
{
	public class BodyTypeResolverBuilder : IBodyTypeResolverBuilder
	{
		private readonly LinkedList<KeyValuePair<FullBodyTypeName, Type>> table = new();
		private string? currentDomain;


		public void BakeToResolver(IBodyTypeResolver resolver)
		{
			resolver.Initialize(table);
		}

		public void RegisterAllias(Type bodyType, string pseudoName)
		{
			if (currentDomain is null)
				throw new InvalidOperationException("Setup domain before add commands");
			table.AddLast(new KeyValuePair<FullBodyTypeName, Type>(new(pseudoName, currentDomain), bodyType));
		}

		public void ResetDomain()
		{
			currentDomain = null;
		}

		public void SetupDomain(string targetDomain)
		{
			currentDomain = targetDomain;
		}
	}
}

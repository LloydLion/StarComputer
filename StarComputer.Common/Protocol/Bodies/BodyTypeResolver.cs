using StarComputer.Common.Abstractions.Protocol.Bodies;

namespace StarComputer.Common.Protocol.Bodies
{
	public class BodyTypeResolver : IBodyTypeResolver
	{
		private readonly Dictionary<FullBodyTypeName, Type> tableF = new();
		private readonly Dictionary<Type, FullBodyTypeName> tableB = new();
		private bool isInitialized = false;


		public void Initialize(IEnumerable<KeyValuePair<FullBodyTypeName, Type>> codeTable)
		{
			foreach (var item in codeTable)
			{
				tableF.Add(item.Key, item.Value);
				tableB.Add(item.Value, item.Key);
			}

			isInitialized = true;
		}

		public FullBodyTypeName Code(Type bodyType)
		{
			IfInitalized();
			return tableB[bodyType];
		}

		public Type Resolve(FullBodyTypeName name)
		{
			IfInitalized();
			return tableF[name];
		}

		private void IfInitalized()
		{
			if (isInitialized == false)
				throw new InvalidOperationException("Initialize resolver before use");
		}
	}
}

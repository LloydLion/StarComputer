namespace StarComputer.Common.Abstractions.Protocol.Bodies
{
	public interface IBodyTypeResolver
	{
		public FullBodyTypeName Code(Type bodyType);

		public Type Resolve(FullBodyTypeName name);

		public void Initialize(IEnumerable<KeyValuePair<FullBodyTypeName, Type>> codeTable);
	}
}

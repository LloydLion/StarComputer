namespace StarComputer.Common.Abstractions.Protocol.Bodies
{
	public interface IBodyTypeResolverBuilder
	{
		public void SetupDomain(string targetDomain);

		public void ResetDomain();

		public void RegisterAllias(Type bodyType, string pseudoName);

		public void BakeToResolver(IBodyTypeResolver resolver);
	}
}

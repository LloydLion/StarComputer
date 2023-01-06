namespace StarComputer.Common.Abstractions.Plugins.Resources
{
	public interface IResourcesManager
	{
		public IPlugin TargetPlugin { get; }


		public FileStream OpenRead(string resourceName);

		public FileStream OpenTemporalFile(string extension);
	}
}

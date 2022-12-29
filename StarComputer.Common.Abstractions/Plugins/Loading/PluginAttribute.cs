namespace StarComputer.Common.Abstractions.Plugins.Loading
{
	[AttributeUsage(AttributeTargets.Class)]
	public class PluginAttribute : Attribute
	{
		public static implicit operator bool(PluginAttribute? attr)
		{
			return attr is not null;
		}
	}
}

namespace StarComputer.Common.Abstractions.Plugins
{
	public record struct PluginDomain(string Domain)
	{
		public static implicit operator string(PluginDomain domain) => domain.Domain;
		public static explicit operator PluginDomain(string domain) => new(domain);
	}
}

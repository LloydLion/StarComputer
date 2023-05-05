using Microsoft.Extensions.Localization;

namespace StarComputer.ApplicationUtils.Localization
{
	public class DesignLocalizer : IStringLocalizer
	{
		public static DesignLocalizer Instance { get; } = new DesignLocalizer();


		private DesignLocalizer() { }


		public LocalizedString this[string name] => new(name, $"[{name}]");

		public LocalizedString this[string name, params object[] arguments] => new(name, $"[{name}|{string.Join("; ", arguments)}]");


		public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
	}
}

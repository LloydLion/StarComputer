using System.Globalization;

namespace StarComputer.ApplicationUtils.Localization
{
	public interface IStarComputerLocalizationProvider
	{
		public Type TargetType { get; }


		public LocaleDictionary GetDictionaryFor(CultureInfo culture);
	}
}

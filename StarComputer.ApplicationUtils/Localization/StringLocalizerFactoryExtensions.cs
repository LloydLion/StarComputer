using Microsoft.Extensions.Localization;

namespace StarComputer.ApplicationUtils.Localization
{
	public static class StringLocalizerFactoryExtensions
	{
		public static IStringLocalizer<TTarget> Create<TTarget>(this IStringLocalizerFactory factory)
		{
			return new LocalizationWrap<TTarget>(factory.Create(typeof(TTarget)));
		}


		private class LocalizationWrap<TTarget> : IStringLocalizer<TTarget>
		{
			private readonly IStringLocalizer localizer;


			public LocalizationWrap(IStringLocalizer localizer)
			{
				this.localizer = localizer;
			}


			public LocalizedString this[string name] => localizer[name];

			public LocalizedString this[string name, params object[] arguments] => localizer[name, arguments];

			public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
			{
				return localizer.GetAllStrings(includeParentCultures);
			}
		}
	}
}

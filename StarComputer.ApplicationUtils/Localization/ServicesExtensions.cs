using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System.Reflection;

namespace StarComputer.ApplicationUtils.Localization
{
	public static class ServicesExtensions
	{
		public static IServiceCollection AddLocalization(this IServiceCollection services, Action<LocalizationOptions> optionsAction, string[] targetAssemblies)
		{
			var callingAssembly = Assembly.GetCallingAssembly().FullName ?? throw new Exception();

			services.Configure(optionsAction);
			services.AddSingleton<ResourceManagerStringLocalizerFactory>();
			services.AddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
			services.AddSingleton<IStringLocalizerFactory, StarComputerLocalizerFactory>(sp =>
				new StarComputerLocalizerFactory(sp.GetRequiredService<ResourceManagerStringLocalizerFactory>(), targetAssemblies, callingAssembly));

			return services;
		}
	}
}

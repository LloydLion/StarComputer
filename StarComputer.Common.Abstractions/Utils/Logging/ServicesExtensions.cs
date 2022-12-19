using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StarComputer.Common.Abstractions.Utils.Logging
{
	public static class ServicesExtensions
	{
		public static ILoggingBuilder AddFancyLogging(this ILoggingBuilder builder)
		{
			builder.Services.AddTransient<ILoggerProvider, FancyLoggerProvider>();
			return builder;
		}
	}
}

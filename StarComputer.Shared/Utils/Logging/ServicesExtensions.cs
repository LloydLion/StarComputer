﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StarComputer.Shared.Utils.Logging
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

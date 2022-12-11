using Microsoft.Extensions.Logging;

namespace StarComputer.Shared.Utils.Logging
{
	public class FancyLoggerProvider : ILoggerProvider
	{
		public ILogger CreateLogger(string categoryName)
		{
			return new FancyLogger(categoryName);
		}

		public void Dispose()
		{
			
		}
	}
}

using Newtonsoft.Json;

namespace StarComputer.Common.Abstractions.Plugins.UI.HTML
{
	public class PageConstructionBag
	{
		public IDictionary<string, string?> ConstructionArguments { get; } = new Dictionary<string, string?>();


		public PageConstructionBag AddConstructionArgument(string key, object argument, bool useJson = false)
		{
			ConstructionArguments.Add(key, useJson ? JsonConvert.SerializeObject(argument) : argument.ToString());
			return this;
		}
	}
}
using Microsoft.Extensions.Localization;
using System.Reflection;

namespace StarComputer.ApplicationUtils.Localization
{
	public sealed class StarComputerLocalizerFactory : IStringLocalizerFactory
	{
		private readonly IStarComputerLocalizationProvider[] instances;
		private readonly IStringLocalizerFactory? delegatedLocalizer;


		public StarComputerLocalizerFactory(string[] targetAssemblies, string callingAssembly)
		{
			var types = targetAssemblies.Append(callingAssembly).Select(s => Assembly.Load(s)).SelectMany(s => s.GetTypes());	

			instances = types.Where(s => s.IsAssignableTo(typeof(IStarComputerLocalizationProvider))
				&& !(s.IsInterface || s.IsValueType || s.IsAbstract || s.IsGenericType) && isInternalOrPublic(s))
				.Select(s => Activator.CreateInstance(s) as IStarComputerLocalizationProvider ?? throw new Exception()).ToArray();



			static bool isInternalOrPublic(Type t)
			{
				return !t.IsNested
					&& !t.IsNestedPublic
					&& !t.IsNestedFamily
					&& !t.IsNestedPrivate
					&& !t.IsNestedAssembly
					&& !t.IsNestedFamORAssem
					&& !t.IsNestedFamANDAssem;
			}
		}

		public StarComputerLocalizerFactory(IStringLocalizerFactory delegatedLocalizer, string[] targetAssemblies, string callingAssembly)
			: this(targetAssemblies, callingAssembly)
		{
			this.delegatedLocalizer = delegatedLocalizer;
		}


		public IStringLocalizer Create(Type resourceSource)
		{
			var instance = instances.SingleOrDefault(s => s.TargetType == resourceSource);
			ILocaleDictionarySource source = instance is null ? new NoSource() : new Source(instance);
			var localizer = delegatedLocalizer?.Create(resourceSource);
			return new StarComputerLocalizer(source, localizer);
		}

		public IStringLocalizer Create(string baseName, string location)
		{
			if (delegatedLocalizer is null)
			{
				var noSource = new NoSource();
				return new StarComputerLocalizer(noSource);
			}
			else
			{
				return delegatedLocalizer.Create(baseName, location);
			}
		}


		private sealed class Source : ILocaleDictionarySource
		{
			private readonly IStarComputerLocalizationProvider provider;


			public Source(IStarComputerLocalizationProvider provider)
			{
				this.provider = provider;
			}


			public LocaleDictionary GetLocaleDictionary()
			{
				return provider.GetDictionaryFor(Thread.CurrentThread.CurrentUICulture);
			}
		}

		private sealed class NoSource : ILocaleDictionarySource
		{
			private readonly LocaleDictionary dic;


			public NoSource()
			{
				dic = new LocaleDictionary(new Dictionary<string, string>());
			}


			public LocaleDictionary GetLocaleDictionary()
			{
				return dic;
			}
		}
	}
}

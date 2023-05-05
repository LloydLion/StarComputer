using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace StarComputer.ApplicationUtils.Localization
{
	public class LocaleDictionary : IReadOnlyDictionary<string, string>
	{
		private readonly IReadOnlyDictionary<string, string> baseDic;


		public LocaleDictionary(IReadOnlyDictionary<string, string> baseDic)
		{
			this.baseDic = baseDic;
		}


		public string this[string key] => baseDic[key];


		public IEnumerable<string> Keys => baseDic.Keys;

		public IEnumerable<string> Values => baseDic.Values;

		public int Count => baseDic.Count;


		public bool ContainsKey(string key)
		{
			return baseDic.ContainsKey(key);
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return baseDic.GetEnumerator();
		}

		public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
		{
			return baseDic.TryGetValue(key, out value);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)baseDic).GetEnumerator();
		}
	}
}

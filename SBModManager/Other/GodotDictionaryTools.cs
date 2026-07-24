using System;
using System.Collections.Generic;
using System.Text;

namespace SBModManager.Other {
	public static class GodotDictionaryTools {

		/// <summary>
		/// Attempts to read a value from the dictionary as a string, strictly. Other types will not be converted,
		/// only <see cref="string"/> and <see cref="StringName"/>.
		/// </summary>
		/// <param name="this"></param>
		/// <param name="key"></param>
		/// <param name="default"></param>
		/// <returns></returns>
		public static string GetValueAsStringOrDefault(this GDDictionary @this, string key, string @default) {
			if (@this.TryGetValue(key, out Variant value)) {
				if (value.VariantType == Variant.Type.String || value.VariantType == Variant.Type.StringName) {
					return (string)value;
				}
			}
			return @default;
		}

		/// <summary>
		/// Gets an existing instance of, or adds a new instance of, a value associated with the provided <paramref name="key"/>.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="this"></param>
		/// <param name="key"></param>
		/// <param name="makeValue"></param>
		/// <returns></returns>
		public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> @this, TKey key, Func<TKey, TValue> makeValue) where TKey : notnull {
			if (@this.TryGetValue(key, out TValue? existing)) {
				return existing;
			} else {
				return @this[key] = makeValue(key);
			}

		}

	}
}

namespace System.Collections.Generic;

public static class DictionaryExtensions
{
    /// <summary>
    /// Gets the value with the given key from the dictionary if it exists. If not,
    /// calls the given creation function and inserts it at the key value.
    /// </summary>
    public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> creator)
    {
        if (!dict.TryGetValue(key, out TValue val))
        {
            val = creator();
            dict.Add(key, val);
        }

        return val;
    }
}

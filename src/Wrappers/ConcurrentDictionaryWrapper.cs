using AIUnitTestWriter.Interfaces;
using System.Collections.Concurrent;

namespace AIUnitTestWriter.Wrappers
{
    public class ConcurrentDictionaryWrapper<TKey, TValue> : IConcurrentDictionaryWrapper<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new();

        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
        public bool TryAdd(TKey key, TValue value) => _dictionary.TryAdd(key, value);
        public bool TryRemove(TKey key, out TValue value) => _dictionary.TryRemove(key, out value);
        public TValue this[TKey key] { get => _dictionary[key]; set => _dictionary[key] = value; }
        public void Clear() => _dictionary.Clear();
        public IEnumerable<KeyValuePair<TKey, TValue>> GetAll() => _dictionary.ToList();
    }
}

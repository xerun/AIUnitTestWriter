namespace AIUnitTestWriter.Interfaces
{
    public interface IConcurrentDictionaryWrapper<TKey, TValue>
    {
        bool TryGetValue(TKey key, out TValue value);
        bool TryAdd(TKey key, TValue value);
        bool TryRemove(TKey key, out TValue value);
        TValue this[TKey key] { get; set; }
        void Clear();
        IEnumerable<KeyValuePair<TKey, TValue>> GetAll();
    }
}

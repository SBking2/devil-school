
using System.Collections.Generic;

namespace EGame
{
    public class Blackboard
    {
        private readonly Dictionary<string, object> _Values = new Dictionary<string, object>();

        public void Set(string key, object value)
        {
            _Values[key] = value;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_Values.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        public T Get<T>(string key, T fallback = default)
        {
            return TryGet<T>(key, out var value) ? value : fallback;
        }

        public bool Has(string key)
        {
            return _Values.ContainsKey(key);
        }

        public void Remove(string key)
        {
            _Values.Remove(key);
        }

        public void Clear()
        {
            _Values.Clear();
        }
    }
}

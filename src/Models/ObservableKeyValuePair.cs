using System.Collections.Generic;

namespace COMPASS.Models
{
    public class ObservableKeyValuePair<K, V> : ObservableObject
    {

        public ObservableKeyValuePair() { }
        public ObservableKeyValuePair(K key, V value)
        {
            Key = key;
            Value = value;
        }
        public ObservableKeyValuePair(KeyValuePair<K, V> pair) : this(pair.Key, pair.Value) { }

        private K _key;
        public K Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        private V _value;
        public V Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}

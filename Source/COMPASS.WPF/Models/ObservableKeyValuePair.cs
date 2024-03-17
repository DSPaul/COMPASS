using System.Collections.Generic;

namespace COMPASS.Models
{
    public class ObservableKeyValuePair<K, V> : ObservableObject
    {
        //empty ctor for deserialization
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ObservableKeyValuePair() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public ObservableKeyValuePair(K key, V value)
        {
            _key = key;
            _value = value;
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

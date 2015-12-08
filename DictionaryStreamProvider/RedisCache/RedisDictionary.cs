using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

// TODO: We can improve the performance using pipelining: _db.CreateTransaction
// This will at least reduce communication. In Python, a speedup of 5x can be achieved http://blog.jmoz.co.uk/python-redis-py-pipeline/
// Will probably have a bigger impact if Redis is running on a different machine.

namespace DictStreamProvider.RedisCache
{
    /// <summary>
    /// A custom data structure with O(1) append/prepend and access. Does not support insertion.
    /// Internally this is using a Redis hash where keys are indices.
    /// </summary>
    public class RedisDictionary<T> : IDictionary<string, T>
    {
        private readonly IDatabase _db;
        private readonly RedisKey _key;
        private readonly Logger _logger;

        public RedisDictionary(IDatabase db, string redisKey, Logger logger)
        {
            _db = db;
            _logger = logger;
            _key = redisKey;

            if (_db.KeyExistsAsync(_key).Result)
            {
                _logger.AutoWarn($"Redis hash already exists with name {_key}. Deleting it.");
                _db.KeyDelete(_key);
            }
        }

        public T Get(string key)
        {
            RedisValue field = key;
            AssertExists(field);
            var bytes = _db.HashGet(_key, field);
            var item = SerializationManager.DeserializeFromByteArray<T>(bytes);
            return item;
        }

        //public T Get(DictStreamToken token)
        //{
        //    if (token.Keys.Length != 1)
        //        throw new DictionaryStreamException("When saving to redis, we must separate a batchcontainer into multiple ones each containing only one event. This is a design choice that makes sense in the case of a dictionary stream");

        //    string key = token.Keys[0];
        //    return Get(key);
        //}

        /// <summary>
        /// Add or update
        /// </summary>
        /// <param name="key"></param>
        /// <param name="@event"></param>
        /// <returns></returns>
        public bool Put(string key, T @event)
        {
            RedisValue field = key;
            var bytes = SerializationManager.SerializeToByteArray(@event);
            RedisValue value = bytes;

            if (!_db.HashSet(_key, field, value))
                return false;
            return true;
        }

        ///// <summary>
        ///// Convenience method. Unback the batch and add all the events in it
        ///// </summary>
        ///// <param name="batch"></param>
        ///// <returns></returns>
        //public bool Put(DictBatchContainer batch)
        //{
        //    // iterate over key (field) -> event (value)
        //    foreach (var tup in batch.GetEvents<T>())
        //    {
        //        if (!(tup.Item2 is DictStreamToken))
        //            throw new Exception("Code must be broken, a DictBatchContainer must use DictSequenceToken");
        //        var token = (tup.Item2 as DictStreamToken);
        //        if (!token.IsOneKey)
        //            throw new Exception($"Code must be broken, a token for one event should contain only one key. Token contains {token.Keys.Length} keys.");

        //        var key = token.Keys[0];
        //        var @event = tup.Item1;

        //        if (!Put(key, @event))
        //            return false;
        //    }
        //    return true;
        //}

        private void AssertExists(string field)
        {
            Debug.Assert(_db.KeyExists(_key) && _db.HashExists(_key, field));
        }

        #region IDictionaryImplementation
        // We only need:
        // Add, [], Count, Keys, ContainsKey
        public ICollection<string> Keys
        {
            get
            {
                // TODO: maybe this could be done in a cheaper way
                var keys = _db.HashKeys(_key);
                var keysAsString = new string[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                    keysAsString[i] = keys[i];

                return keysAsString;
            }
        }

        public T this[string key]
        {
            get
            {
                return Get(key);
            }

            set
            {
                Put(key, value);
            }
        }
        public int Count
        {
            get
            {
                var len = _db.HashLength(_key);
                // WARNING: casting long to int
                return (int)len;
            }
        }

        public bool ContainsKey(string key)
        {
            return _db.HashExists(_key, key);
        }

        public void Add(string key, T value)
        {
            Put(key, value);
        }
        public void Add(KeyValuePair<string, T> item)
        {
            Add(item.Key, item.Value);
        }

        // We don't need any of the following functions
        public ICollection<T> Values
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out T value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<string, T> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, T> item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

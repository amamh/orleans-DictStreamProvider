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

// TODO: We can improve the performance using pipelining: _db.CreateTransaction
// This will at least reduce communication. In Python, a speedup of 5x can be achieved http://blog.jmoz.co.uk/python-redis-py-pipeline/
// Will probably have a bigger impact if Redis is running on a different machine.

namespace DictStreamProvider.RedisCache
{
    /// <summary>
    /// A custom data structure with O(1) append/prepend and access. Does not support insertion.
    /// Internally this is using a Redis hash where keys are indices.
    /// </summary>
    public class RedisCustomList<T> : IRedisCache<T>
    {
        private readonly IDatabase _db;
        private readonly RedisKey _key;
        private readonly string _keySet;
        private readonly Logger _logger;
        private long _lastIndx = 0;
        private long _firstIndx = 0;

        public long Count { get; private set; } = 0;

        public RedisCustomList(IDatabase db, string redisKey, Logger logger)
        {
            _db = db;
            _logger = logger;
            _key = redisKey;
            _keySet = $"{redisKey}_set";

            if (_db.KeyExistsAsync(_key).Result)
            {
                _logger.AutoWarn($"Redis hash already exists with name {_key}. Deleting it.");
                _db.KeyDelete(_key);
            }
            if (_db.KeyExistsAsync(_keySet).Result)
            {
                _logger.AutoWarn($"Redis companion set for hash {_key} already exists with name {_keySet}. Deleting it.");
                _db.KeyDelete(_keySet);
            }
        }

        public T Get(DictSequenceToken token)
        {
            if (token.Keys.Length != 1)
                throw new DictionaryStreamException("When saving to redis, we must separate a batchcontainer into multiple ones each containing only one event. This is a design choice that makes sense in the case of a dictionary stream");

            RedisValue field = token.Keys[0];

            AssertExists(field);

            var bytes = _db.HashGet(_key, field);
            var item = SerializationManager.DeserializeFromByteArray<T>(bytes);
            return item;
        }

        public bool Put(DictBatchContainer batch)
        {
            for (int i = 0; i < batch.EventsCount; i++)
            {
                RedisValue field = batch.TypedSequenceToken.Keys[i];
                var @event = batch.Events[i];
                var bytes = SerializationManager.SerializeToByteArray(@event);
                RedisValue value = bytes;

                if (!_db.HashSet(_key, field, value))
                    return false;
            }
            return true;
        }

        private void AssertExists(string field)
        {
            Debug.Assert(_db.KeyExists(_key) && _db.HashExists(_key, field));
        }

    }
}

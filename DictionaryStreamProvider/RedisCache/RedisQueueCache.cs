using Orleans;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Orleans.Providers.Streams.Common;

namespace DictStreamProvider.RedisCache
{
    public class QueueCacheRedis : IQueueCache
    {
        private readonly Logger _logger;
        private readonly RedisCustomList<IBatchContainer> _cache;

        public QueueId Id { get; }
        public int MaxAddCount { get; } = 1024; // some sensible number

        public int Size => (int)_cache.Count; // WARNING

        public QueueCacheRedis(QueueId id, Logger logger, IDatabase db)
        {
            Id = id;
            _logger = logger;
            string redisHashName = $"{Environment.MachineName}-{DateTime.UtcNow}-orleans-pipecache-{id}";
            _cache = new RedisCustomList<IBatchContainer>(db, redisHashName, _logger);
        }

        public void AddToCache(IList<IBatchContainer> messages)
        {
            foreach (var item in messages)
                if (!_cache.RightPush(item))
                    _logger.AutoError($"Couldn't add batch to cache. Namespace: {item.StreamNamespace}, Stream: {item.StreamGuid}, Queue Cache ID: {Id}");
        }

        public IQueueCacheCursor GetCacheCursor(Guid streamGuid, string streamNamespace, StreamSequenceToken token)
        {
            if (token != null && !(token is SimpleSequenceToken))
            {
                // Null token can come from a stream subscriber that is just interested to start consuming from latest (the most recent event added to the cache).
                throw new ArgumentOutOfRangeException("token", "token must be of type SimpleSequenceToken");
            }

            return new QueueCacheRedisCursor(_cache, streamNamespace, streamGuid, token as SimpleSequenceToken);
        }

        public bool IsUnderPressure()
        {
            // FIXME
            return false;
        }

        public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
        {
            // FIXME
            purgedItems = null;

            return true;
        }
    }
}
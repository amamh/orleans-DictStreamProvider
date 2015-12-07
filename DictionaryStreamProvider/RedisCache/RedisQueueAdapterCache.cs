using Orleans.Runtime;
using Orleans.Streams;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictStreamProvider.RedisCache
{
    public class QueueAdapterCacheRedis : IQueueAdapterCache
    {
        private readonly Logger _logger;
        private readonly IDatabase _db;
        private readonly ConcurrentDictionary<QueueId, QueueCacheRedis> _caches;

        public int Size
        {
            // TODO: Optimise
            get { return _caches.Select(pair => pair.Value.Size).Sum(); }
        }

        public QueueAdapterCacheRedis(Logger logger, IDatabase db)
        {
            _logger = logger;
            _db = db;
            _caches = new ConcurrentDictionary<QueueId, QueueCacheRedis>();
        }

        public IQueueCache CreateQueueCache(QueueId queueId)
        {
            throw new NotImplementedException();
        }
    }
}

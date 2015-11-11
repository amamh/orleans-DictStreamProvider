using System;
using System.Collections.Concurrent;
using System.Linq;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace DictStreamProvider
{
    class DictQueueAdapterCache : IQueueAdapterCache
    {
        private readonly int _cacheSize;
        private readonly Logger _logger;
        private readonly ConcurrentDictionary<QueueId, IQueueCache> _caches;

        public DictQueueAdapterCache(IQueueAdapterFactory factory, int cacheSize, Logger logger)
        {
            if (cacheSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(cacheSize), "CacheSize must be a positive number.");
            this._cacheSize = cacheSize;
            this._logger = logger;
            _caches = new ConcurrentDictionary<QueueId, IQueueCache>();
        }

        public IQueueCache CreateQueueCache(QueueId queueId)
        {
            return _caches.AddOrUpdate(queueId, (id) => new DictQueueCache(id, _cacheSize, _logger), (id, queueCache) => queueCache);
        }

        public int Size
        {
            get { return _caches.Select(pair => pair.Value.Size).Sum(); }
        }
    }
}
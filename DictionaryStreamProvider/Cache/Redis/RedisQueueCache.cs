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
using IterableDictionary;

namespace DictStreamProvider.Cache.Redis
{
    public class QueueCacheRedis : Memory.DictQueueCache
    {
        private readonly IDatabase _db;

        public QueueCacheRedis(QueueId id, Logger logger, IDatabase db) : base(id, logger)
        {
            _db = db;
        }

        override protected IterableDict<string, DictBatchContainer> DictFactory(string streamNamespace, Guid streamGuid)
        {
            string redisHashName = $"{Environment.MachineName}-{DateTime.UtcNow}-orleans-dictcache-{streamNamespace}-{streamGuid}";
            var redisDict = new RedisDictionary<DictBatchContainer>(_db, redisHashName, _logger);
            var iterDict = new IterableDict<string, DictBatchContainer>(redisDict);

            return iterDict;
        }
    }
}
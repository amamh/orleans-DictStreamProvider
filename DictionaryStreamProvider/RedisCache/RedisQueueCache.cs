﻿using Orleans;
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

namespace DictStreamProvider.RedisCache
{
    public class QueueCacheRedis : MemoryCache.DictQueueCache
    {
        private readonly RedisDictionary<DictBatchContainer> _redisDict;

        public QueueCacheRedis(QueueId id, Logger logger, IDatabase db) : base(id, logger)
        {
            string redisHashName = $"{Environment.MachineName}-{DateTime.UtcNow}-orleans-dictcache-{id}";
            _redisDict = new RedisDictionary<DictBatchContainer>(db, redisHashName, _logger);
            var cache = new IterableDict<string, DictBatchContainer>(_redisDict);

            SetDict(cache);
        }
    }
}
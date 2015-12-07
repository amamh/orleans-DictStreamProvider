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
        public QueueId Id
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int MaxAddCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Size
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void AddToCache(IList<IBatchContainer> messages)
        {
            throw new NotImplementedException();
        }

        public IQueueCacheCursor GetCacheCursor(Guid streamGuid, string streamNamespace, StreamSequenceToken token)
        {
            throw new NotImplementedException();
        }

        public bool IsUnderPressure()
        {
            throw new NotImplementedException();
        }

        public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
        {
            throw new NotImplementedException();
        }
    }
}
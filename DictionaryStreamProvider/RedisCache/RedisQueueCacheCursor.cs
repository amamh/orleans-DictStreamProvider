using Orleans.Providers.Streams.Common;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictStreamProvider.RedisCache
{
    public class QueueCacheRedisCursor : IQueueCacheCursor
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IBatchContainer GetCurrent(out Exception exception)
        {
            throw new NotImplementedException();
        }

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public void Refresh()
        {
            throw new NotImplementedException();
        }
    }
}

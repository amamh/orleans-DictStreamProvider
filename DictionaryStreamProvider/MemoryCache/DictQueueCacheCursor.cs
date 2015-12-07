using System;
using System.Collections.Generic;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;
using IterableDictionary;

namespace DictStreamProvider.MemoryCache
{
    public class DictQueueCacheCursor : IQueueCacheCursor
    {
        private readonly IterableLinkedListCursor<string> _curosr;
        private readonly IterableDict<string, DictBatchContainer> _dict;

        public DictQueueCacheCursor (IterableLinkedListCursor<string> internalCursor, IterableDict<string, DictBatchContainer> dict)
        {
            _curosr = internalCursor;
            _dict = dict;
        }

        public IBatchContainer GetCurrent(out Exception exception)
        {
            try {
                exception = null;
                return _dict[_curosr.GetCurrent()];
            }
            catch (Exception ex)
            {
                exception = ex;
                return null;
            }
        }

        public bool MoveNext()
        {
            return _curosr.MoveNext();
        }

        public void Refresh()
        {
            // nothing to do
        }

        public void Dispose()
        {
            // TODO: maybe we can dispose _cursor?
        }
    }
}
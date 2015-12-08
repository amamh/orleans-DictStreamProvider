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
                var result = _dict[_curosr.GetCurrent()];
                return result;
            }
            catch (Exception ex)
            {
                exception = ex;
                return null;
            }
        }

        public bool MoveNext()
        {
            var result = _curosr.MoveNext();
            return result;
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
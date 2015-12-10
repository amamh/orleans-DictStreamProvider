using System;
using System.Collections.Generic;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;
using IterableDictionary;
//using System.Diagnostics;

namespace DictStreamProvider.MemoryCache
{
    public class DictQueueCacheCursor : IQueueCacheCursor
    {
        private readonly IterableLinkedListCursor<string> _curosr;
        private readonly IterableDict<string, DictBatchContainer> _dict;
        private readonly string _streamNamespace;
        private readonly Guid _streamGuid;

        public DictQueueCacheCursor(IterableLinkedListCursor<string> internalCursor, IterableDict<string, DictBatchContainer> dict, string streamNamespace, Guid streamGuid)
        {
            _curosr = internalCursor;
            _dict = dict;
            _streamNamespace = streamNamespace;
            _streamGuid = streamGuid;
        }

        public IBatchContainer GetCurrent(out Exception exception)
        {
            try {
                exception = null;
                var result = _dict[_curosr.GetCurrent()];
                //Debug.Assert(result.TypedSequenceToken.IsOneKey);
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
            while(_curosr.MoveNext())
            {
                // TODO: We are doing this again later, cache it
                // TODO: Exception handling
                var batch = _dict[_curosr.GetCurrent()];
                if (batch.StreamNamespace == _streamNamespace && batch.StreamGuid == _streamGuid)
                    return true;
            }

            return false;
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
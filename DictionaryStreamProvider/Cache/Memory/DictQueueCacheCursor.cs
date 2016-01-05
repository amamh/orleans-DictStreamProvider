using System;
using System.Collections.Generic;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;
using IterableDictionary;
using System.Diagnostics;
//using System.Diagnostics;

namespace DictStreamProvider.Cache.Memory
{
    public class DictQueueCacheCursor : IQueueCacheCursor
    {
        private readonly IterableLinkedListCursor<string> _curosr;
        private readonly IterableDict<string, DictBatchContainer> _dict;
        private readonly string _streamNamespace;
        private readonly Guid _streamGuid;

        public DictQueueCacheCursor(IterableDict<string, DictBatchContainer> dict, string streamNamespace, Guid streamGuid)
        {
            _dict = dict;
            _curosr = _dict.GetCursor();
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
            // No need to check the batch for whether it's relevant to this stream or not, since there is a separate dictionary per stream, so this cursor is only for one stream
            var result = _curosr.MoveNext();
#if DEBUG
            if (result) // make sure this cursor is indeed interating over messages only for the relevant stream
            {
                var batch = _dict[_curosr.GetCurrent()];
                Debug.Assert(batch.StreamNamespace == _streamNamespace && batch.StreamGuid == _streamGuid);
            }
#endif
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
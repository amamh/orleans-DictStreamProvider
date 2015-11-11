using System;
using System.Collections.Generic;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace DictStreamProvider
{
    public class DictQueueCacheCursor : IQueueCacheCursor
    {
        private readonly Guid _streamGuid;
        private readonly string _streamNamespace;
        private readonly DictQueueCache _cache;
        private readonly Logger _logger;
        private IBatchContainer _current; // this is a pointer to the current element in the cache. It is what will be returned by GetCurrent().

        // This is a pointer to the NEXT element in the cache.
        // After the cursor is first created it should be called MoveNext before the call to GetCurrent().
        // After MoveNext returns, the current points to the current element that will be returned by GetCurrent()
        // and Element will point to the next element (since MoveNext actualy advanced it to the next).
        internal LinkedListNode<SimpleQueueCacheItem> Element { get; private set; }
        internal StreamSequenceToken SequenceToken { get; private set; }

        internal bool IsSet => Element != null;

        internal void Reset(StreamSequenceToken token)
        {
            Element = null;
            SequenceToken = token;
        }

        internal void Set(LinkedListNode<SimpleQueueCacheItem> item)
        {
            Element = item;
            SequenceToken = item.Value.SequenceToken;
        }

        public DictQueueCacheCursor(DictQueueCache cache, Guid streamGuid, string streamNamespace, Logger logger)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }
            this._cache = cache;
            this._streamGuid = streamGuid;
            this._streamNamespace = streamNamespace;
            this._logger = logger;
            _current = null;
            DictQueueCache.Log(logger, "SimpleQueueCacheCursor New Cursor for {0}, {1}", streamGuid, streamNamespace);
        }

        public virtual IBatchContainer GetCurrent(out Exception exception)
        {
            DictQueueCache.Log(_logger, "SimpleQueueCacheCursor.GetCurrent: {0}", _current);

            exception = null;
            return _current;
        }

        public virtual bool MoveNext()
        {
            IBatchContainer next;
            while (_cache.TryGetNextMessage(this, out next))
            {
                if (IsInStream(next))
                    break;
            }
            if (!IsInStream(next))
                return false;

            _current = next;
            return true;
        }

        public virtual void Refresh()
        {
            if (!IsSet)
            {
                _cache.InitializeCursor(this, SequenceToken);
            }
        }

        private bool IsInStream(IBatchContainer batchContainer)
        {
            return batchContainer != null &&
                    batchContainer.StreamGuid.Equals(_streamGuid) &&
                    String.Equals(batchContainer.StreamNamespace, _streamNamespace);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cache.ResetCursor(this, null);
            }
        }

        #endregion

        public override string ToString()
        {
            return string.Format("<SimpleQueueCacheCursor: Element={0}, SequenceToken={1}>",
                Element != null ? Element.Value.Batch.ToString() : "null", SequenceToken != null ? SequenceToken.ToString() : "null");
        }
    }
}
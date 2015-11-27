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
        internal StreamSequenceToken OldestPossibleToken { get; } = new SimpleSequenceToken(0);

        private readonly RedisCustomList<IBatchContainer> _cache;
        private readonly string _namespace;
        private readonly Guid _stream;
        private readonly SimpleSequenceToken _startToken;
        private long _index = 0;

        public QueueCacheRedisCursor(RedisCustomList<IBatchContainer> cache, string streamNamespace, Guid streamGuid, SimpleSequenceToken token)
        {
            if (token.Older(OldestPossibleToken))
                throw new QueueCacheMissException($"Can't ask for a token older than SimpleSequenceToken(0). Requested token:\n{token}");

            _cache = cache;
            _namespace = streamNamespace;
            _stream = streamGuid;
            _startToken = token;
            _index = _startToken == null ? 0 : _startToken.SequenceNumber;
        }
        public IBatchContainer GetCurrent(out Exception exception)
        {
            try
            {
                exception = null;
                var batch = _cache.Get(_index);
                return batch;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
        }

        public bool MoveNext()
        {
            IBatchContainer next;
            while (_index < _cache.Count)
            {
                _index++;
                next = _cache.Get(_index); // TODO: we will do this retrieval again laster in GetCurrent, maybe we can cache it

                if (next?.StreamNamespace == _namespace && next?.StreamGuid == _stream)
                    return true;
            }
            return false;
        }

        public void Refresh()
        {
            // nothing to do here
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~QueueCacheRedisCursor() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

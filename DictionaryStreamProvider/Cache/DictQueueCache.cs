using Orleans;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using IterableDictionary;
using System.Collections.Concurrent;

namespace DictStreamProvider.Cache
{
    // TODO: Create a dictoinary PER stream
    public class DictQueueCache : IQueueCache
    {
        protected readonly Logger _logger;
        protected ConcurrentDictionary<string, ConcurrentDictionary<Guid, IterableDict<string, DictBatchContainer>>> _dicts;

        public QueueId Id { get; }
        public int MaxAddCount { get; } = 1024; // some sensible number

        // This will be removed in the future
        public int Size
        {
            get
            {
                var total = 0;
                foreach (var item in _dicts.Values)
                    foreach (var dict in item.Values)
                        total += dict.Size;
                return total;
            }
        }

        public DictQueueCache(QueueId id, Logger logger)
        {
            Id = id;
            _logger = logger;
            _dicts = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, IterableDict<string, DictBatchContainer>>>();
        }

        virtual protected IterableDict<string, DictBatchContainer> DictFactory(string ns, Guid guid) => new IterableDict<string, DictBatchContainer>();

        public void AddToCache(IList<IBatchContainer> messages)
        {
            // when adding, we separate every batch into batches each containing only one event
            foreach (var batch in messages)
            {
                if (!(batch is DictBatchContainer))
                    throw new Exception("Code must be broken, a message is not a DictBatchContainer. Must use only DictBatchContainer.");

                var dict = GetDict(batch.StreamNamespace, batch.StreamGuid);
                var typedBatch = batch as DictBatchContainer;
                foreach (var smallBatch in typedBatch.BatchPerEvent)
                {
                    var key = smallBatch.TypedSequenceToken.Keys[0];
                    var value = smallBatch;
                    dict.AddOrUpdate(key, value);
                }
            }
        }

        public IQueueCacheCursor GetCacheCursor(Guid streamGuid, string streamNamespace, StreamSequenceToken token)
        {
            // NOTE: We assume the client ALWAYS wants to replay the whole dictionary, if it doesn't then it shouldn't be using this stream type in the first place.
            if (token != null && !(token is DictStreamToken))
            {
                // Null token can come from a stream subscriber that is just interested to start consuming from latest (the most recent event added to the cache).
                throw new ArgumentOutOfRangeException("token", "token must be of type DictStreamToken");
            }
            var dict = GetDict(streamNamespace, streamGuid);
            var dictCursor = dict.GetCursor();
            return new DictQueueCacheCursor(dict, streamNamespace, streamGuid);
        }

        private IterableDict<string, DictBatchContainer> GetDict(string streamNamespace, Guid streamGuid)
        {
            var dictForNamespace = _dicts.GetOrAdd(streamNamespace, new ConcurrentDictionary<Guid, IterableDict<string, DictBatchContainer>>());
            var dict = dictForNamespace.GetOrAdd(streamGuid, (Guid guid) => DictFactory(streamNamespace, guid) );
            return dict;
        }

        public bool IsUnderPressure()
        {
            // TODO: How to measure pressure?
            return false;
        }

        public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
        {
            // We don't purge
            purgedItems = null;
            return true;
        }
    }
}
using Orleans;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using IterableDictionary;

namespace DictStreamProvider.MemoryCache
{
    public class DictQueueCache : IQueueCache
    {
        protected readonly Logger _logger;
        protected IterableDict<string, DictBatchContainer> _dict;

        public QueueId Id { get; }
        public int MaxAddCount { get; } = 1024; // some sensible number

        public int Size => _dict.Size;

        public DictQueueCache(QueueId id, Logger logger)
        {
            if (id.GetNumericId() > 0)
                throw new DictionaryStreamException("Id is greater than 0, this means there are more than one cache. This shouldn't happen in this type of stream.");

            Id = id;
            _logger = logger;
            SetDict(new IterableDict<string, DictBatchContainer>());
        }

        protected void SetDict(IterableDict<string, DictBatchContainer> dict)
        {
            _dict = dict;
        }

        public void AddToCache(IList<IBatchContainer> messages)
        {
            // when adding, we separate every batch into batches each containing only one event
            foreach (var batch in messages)
            {
                if (!(batch is DictBatchContainer))
                    throw new Exception("Code must be broken, a message is not a DictBatchContainer. Must use only DictBatchContainer.");

                var typedBatch = batch as DictBatchContainer;
                foreach (var smallBatch in typedBatch.BatchPerEvent)
                {
                    var key = smallBatch.TypedSequenceToken.Keys[0];
                    var value = smallBatch;
                    _dict.AddOrUpdate(key, value);
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

            var dictCursor = _dict.GetCursor();
            return new DictQueueCacheCursor(dictCursor, _dict);
        }

        public bool IsUnderPressure()
        {
            // FIXME
            return false;
        }

        public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
        {
            // FIXME
            purgedItems = null;

            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;
using System.Diagnostics;

namespace DictStreamProvider
{
    internal class CacheBucket
    {
        // For backpressure detection we maintain a histogram of 10 buckets.
        // Every buckets recors how many items are in the cache in that bucket
        // and how many cursors are poinmting to an item in that bucket.
        // We update the NumCurrentItems when we add and remove cache item (potentially opening or removing a bucket)
        // We update NumCurrentCursors every time we move a cursor
        // If the first (most outdated bucket) has at least one cursor pointing to it, we say we are under back pressure (in a full cache).
        internal int NumCurrentItems { get; private set; }
        internal int NumCurrentCursors { get; private set; }

        internal void UpdateNumItems(int val)
        {
            NumCurrentItems = NumCurrentItems + val;
        }
        internal void UpdateNumCursors(int val)
        {
            NumCurrentCursors = NumCurrentCursors + val;
        }
    }

    internal struct SimpleQueueCacheItem
    {
        internal IBatchContainer Batch;
        internal StreamSequenceToken SequenceToken;
        internal CacheBucket CacheBucket;
    }

    public class DictQueueCache : IQueueCache
    {
        private readonly LinkedList<SimpleQueueCacheItem> _cachedMessages;

        private readonly LinkedList<SimpleQueueCacheItem> _oldData;
        private readonly Dictionary<string, LinkedListNode<SimpleQueueCacheItem>> _oldDataLookup;

        private StreamSequenceToken _lastSequenceTokenAddedToCache;
        private readonly int _maxCacheSize;
        private readonly Logger _logger;
        private readonly List<CacheBucket> _cacheCursorHistogram; // for backpressure detection
        private const int NumCacheHistogramBuckets = 100;

        public QueueId Id { get; private set; }

        internal EventSequenceToken OldestPossibleToken { get; } = new EventSequenceToken(0);
        internal SimpleQueueCacheItem? LastMessage => _cachedMessages?.First?.Value ?? _oldData?.First?.Value;

        public int Size => _cachedMessages.Count;

        public int MaxAddCount { get; }

        public DictQueueCache(QueueId queueId, int cacheSize, Logger logger)
        {
            Id = queueId;
            _cachedMessages = new LinkedList<SimpleQueueCacheItem>();
            _oldData = new LinkedList<SimpleQueueCacheItem>();
            _oldDataLookup = new Dictionary<string, LinkedListNode<SimpleQueueCacheItem>>();
            _maxCacheSize = cacheSize;

            this._logger = logger;
            _cacheCursorHistogram = new List<CacheBucket>();
            MaxAddCount = Math.Max(cacheSize / NumCacheHistogramBuckets, 1); // we have 10 buckets
        }

        public virtual bool IsUnderPressure()
        {
            if (_cachedMessages.Count == 0) return false; // empty cache
            if (Size < _maxCacheSize) return false; // there is still space in cache
            if (_cacheCursorHistogram.Count == 0) return false;    // no cursors yet - zero consumers basically yet.
            // cache is full. Check how many cursors we have in the oldest bucket.
            int numCursorsInLastBucket = _cacheCursorHistogram[0].NumCurrentCursors;
            return numCursorsInLastBucket > 0;
        }

        public virtual bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
        {
            purgedItems = null;
            if (_cachedMessages.Count == 0) return false; // empty cache
            if (_cacheCursorHistogram.Count == 0) return false;  // no cursors yet - zero consumers basically yet.
            if (_cacheCursorHistogram[0].NumCurrentCursors > 0) return false; // consumers are still active in the oldest bucket - fast path

            var allItems = new List<IBatchContainer>();
            while (_cacheCursorHistogram.Count > 0 && _cacheCursorHistogram[0].NumCurrentCursors == 0)
            {
                List<IBatchContainer> items = EmptyBucket(_cacheCursorHistogram[0]);
                allItems.AddRange(items);
                _cacheCursorHistogram.RemoveAt(0); // remove the last bucket
            }
            purgedItems = allItems;
            return true;
        }

        private List<IBatchContainer> EmptyBucket(CacheBucket bucket)
        {
            var itemsToRelease = new List<IBatchContainer>(bucket.NumCurrentItems);
            // walk all items in the cache starting from last
            // and remove from the cache the oness that reside in the given bucket until we jump to a next bucket
            while (bucket.NumCurrentItems > 0)
            {
                SimpleQueueCacheItem item = _cachedMessages.Last.Value;
                if (item.CacheBucket.Equals(bucket))
                {
                    itemsToRelease.Add(item.Batch);
                    bucket.UpdateNumItems(-1);
                    _cachedMessages.RemoveLast();
                    // Save for replay
                    Save(item);
                }
                else
                {
                    // this item already points into the next bucket, so stop.
                    break;
                }
            }
            return itemsToRelease;
        }

        public virtual void AddToCache(IList<IBatchContainer> msgs)
        {
            if (msgs == null) throw new ArgumentNullException(nameof(msgs));

            Log(_logger, "AddToCache: added {0} items to cache.", msgs.Count);
            foreach (var message in msgs)
            {
                Add(message, message.SequenceToken);
                _lastSequenceTokenAddedToCache = message.SequenceToken;
            }
        }

        public virtual IQueueCacheCursor GetCacheCursor(Guid streamGuid, string streamNamespace, StreamSequenceToken token)
        {
            if (token != null && !(token is EventSequenceToken))
            {
                // Null token can come from a stream subscriber that is just interested to start consuming from latest (the most recent event added to the cache).
                throw new ArgumentOutOfRangeException(nameof(token), "token must be of type EventSequenceToken");
            }

            var cursor = new DictQueueCacheCursor(this, streamGuid, streamNamespace, _logger);
            InitializeCursor(cursor, token);
            return cursor;
        }

        internal void InitializeCursor(DictQueueCacheCursor cursor, StreamSequenceToken sequenceToken)
        {
            Log(_logger, "InitializeCursor: {0} to sequenceToken {1}", cursor, sequenceToken);
            // if offset is not set, iterate from newest (first) message in cache, but not including the first message itself
            if (sequenceToken == null)
            {
                var tokenToReset = ((EventSequenceToken)_lastSequenceTokenAddedToCache)?.NextSequenceNumber();
                ResetCursor(cursor, tokenToReset);
                return;
            }

            // Can't ask for < 0
            if (sequenceToken.Older(OldestPossibleToken))
                throw new QueueCacheMissException($"Requested token is too old: {sequenceToken}. You cannot request earlier than: {OldestPossibleToken}");

            var lastToken = LastMessage?.SequenceToken;
            var oldestToken = _cachedMessages.Last?.Value.SequenceToken;

            if (sequenceToken.Equals(OldestPossibleToken)) // 0
            {
                if (_oldData.Count != 0)
                    SetCursor(cursor, _oldData.Last);
                else
                    // wait
                    ResetCursor(cursor, sequenceToken);
            }
            // sequenceId is too new to be in cache
            else if (_cachedMessages.Count == 0 || sequenceToken.Newer(lastToken))
            {
                // hasn't reached that point yet, retry
                ResetCursor(cursor, sequenceToken);
                return;
            }
            else if (Within(sequenceToken, _cachedMessages))
            {
                var node = _cachedMessages.First;
                // Now the requested sequenceToken is set and is also within the limits of the cache.

                // Find first message at or below offset
                // Events are ordered from newest to oldest, so iterate from start of list until we hit a node at a previous offset, or the end.
                while (node != null && node.Value.SequenceToken.Newer(sequenceToken))
                {
                    // did we get to the end?
                    if (node.Next == null) // node is the last message
                        break;

                    // if sequenceId is between the two, take the higher
                    if (node.Next.Value.SequenceToken.Older(sequenceToken))
                        break;

                    node = node.Next;
                }
                SetCursor(cursor, node);
            }
            else if (sequenceToken.Older(oldestToken))
            {
                _logger.Warn(-1, $"Warning: Trying to initialise with a token that's not in the cache and not equal to 0: {sequenceToken}, oldest in current cache: {oldestToken}, current old data count: {_oldData.Count}\nWill replay all again by setting cursor to 0");
                ResetCursor(cursor, OldestPossibleToken);
            }
            else // Shouldn't happen
            {
                throw new QueueCacheMissException($"old data logic failure in InitializeCursor. Requested token {sequenceToken}, oldest in current cache: {oldestToken}, current old data count: {_oldData.Count}");
            }
        }

        /// <summary>
        /// Aquires the next message in the cache at the provided cursor
        /// </summary>
        /// <param name="cursor"></param>
        /// <param name="batch"></param>
        /// <returns></returns>
        internal bool TryGetNextMessage(DictQueueCacheCursor cursor, out IBatchContainer batch)
        {
            Log(_logger, "TryGetNextMessage: {0}", cursor);

            batch = null;

            if (cursor == null) throw new ArgumentNullException(nameof(cursor));

            //if not set, try to set and then get next
            if (!cursor.IsSet)
            {
                InitializeCursor(cursor, cursor.SequenceToken);
                return cursor.IsSet && TryGetNextMessage(cursor, out batch);
            }

            Debug.Assert(cursor.SequenceToken.Newer(OldestPossibleToken) || cursor.SequenceToken.Equals(OldestPossibleToken));

            // Cursor now points to a valid message in the cache. Get it!
            // Capture the current element and advance to the next one.
            batch = cursor.Element.Value.Batch;
            var nextToken = ((EventSequenceToken)cursor.SequenceToken).NextSequenceNumber();

            // Advance to next:
            if (cursor.Element == _cachedMessages.First)
            {
                // If we are at the end of the cache unset cursor and move offset one forward
                ResetCursor(cursor, nextToken);
            }
            else // move to next
            {
                // Done with old data?
                if (cursor.Element == _oldData.First)
                {
                    // Hot cache has nothing at the moment i.e. the cursor is now in sync
                    if (_cachedMessages.Count == 0)
                        // Wait next one
                        ResetCursor(cursor, nextToken);
                    // There is something in hot cache, start replaying that i.e. set the cursor to the start of the hot cache
                    else
                        UpdateCursor(cursor, _cachedMessages.Last); ;
                }
                else // advance to next
                {
                    // it can only be null if this is the latest element in either of the caches. We have checked for that already
                    Debug.Assert(cursor.Element.Previous != null);
                    UpdateCursor(cursor, cursor.Element.Previous);
                }
            }

            return true;
        }

        private void UpdateCursor(DictQueueCacheCursor cursor, LinkedListNode<SimpleQueueCacheItem> item)
        {
            Log(_logger, "UpdateCursor: {0} to item {1}", cursor, item.Value.Batch);

            cursor.Element.Value.CacheBucket.UpdateNumCursors(-1); // remove from prev bucket
            cursor.Set(item);
            cursor.Element.Value.CacheBucket.UpdateNumCursors(1);  // add to next bucket
        }

        internal void SetCursor(DictQueueCacheCursor cursor, LinkedListNode<SimpleQueueCacheItem> item)
        {
            Log(_logger, "SetCursor: {0} to item {1}", cursor, item.Value.Batch);

            cursor.Set(item);
            cursor.Element.Value.CacheBucket.UpdateNumCursors(1);  // add to next bucket
        }

        internal void ResetCursor(DictQueueCacheCursor cursor, StreamSequenceToken token)
        {
            Log(_logger, "ResetCursor: {0} to token {1}", cursor, token);

            if (cursor.IsSet)
            {
                cursor.Element.Value.CacheBucket.UpdateNumCursors(-1);
            }
            cursor.Reset(token);
        }

        private void Add(IBatchContainer batch, StreamSequenceToken sequenceToken)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));

            CacheBucket cacheBucket = null;
            if (_cacheCursorHistogram.Count == 0)
            {
                cacheBucket = new CacheBucket();
                _cacheCursorHistogram.Add(cacheBucket);
            }
            else
            {
                cacheBucket = _cacheCursorHistogram[_cacheCursorHistogram.Count - 1]; // last one
            }

            if (cacheBucket.NumCurrentItems == MaxAddCount) // last bucket is full, open a new one
            {
                cacheBucket = new CacheBucket();
                _cacheCursorHistogram.Add(cacheBucket);
            }

            // Add message to linked list
            var item = new SimpleQueueCacheItem
            {
                Batch = batch,
                SequenceToken = sequenceToken,
                CacheBucket = cacheBucket
            };

            _cachedMessages.AddFirst(new LinkedListNode<SimpleQueueCacheItem>(item));
            cacheBucket.UpdateNumItems(1);

            if (Size > _maxCacheSize)
            {
                //var last = cachedMessages.Last;
                _cachedMessages.RemoveLast();
                var bucket = _cacheCursorHistogram[0]; // same as:  var bucket = last.Value.CacheBucket;
                bucket.UpdateNumItems(-1);
                if (bucket.NumCurrentItems == 0)
                {
                    _cacheCursorHistogram.RemoveAt(0);
                }
            }
        }

        private void Save(SimpleQueueCacheItem item)
        {
            var batches = item.Batch.GetEvents<IObjectWithUniqueId<int>>();

            foreach (var tuple in batches)
            {
                var message = tuple.Item1;
                if (_oldDataLookup.ContainsKey(message.Id))
                {
                    var node = _oldDataLookup[message.Id];
                    _oldDataLookup[message.Id].Value = item;
                    // Move it back to head so that whoever is in the middle of reading the queue will get the new value
                    _oldData.Remove(node);
                    _oldData.AddFirst(node);
                }
                else
                {
                    var node = new LinkedListNode<SimpleQueueCacheItem>(item);
                    _oldDataLookup.Add(message.Id, node);
                    _oldData.AddFirst(node);
                }
            }
        }

        internal static void Log(Logger logger, string format, params object[] args)
        {
            if (logger.IsVerbose) logger.Verbose(format, args);
            //if(logger.IsInfo) logger.Info(format, args);
        }

        internal static bool Within(StreamSequenceToken token, LinkedList<SimpleQueueCacheItem> queue)
        {
            var oldest = queue.Last.Value.SequenceToken;
            var earliest = queue.First.Value.SequenceToken;

            return ((token.Newer(oldest) && token.Older(earliest)) || token.Equals(oldest) || token.Equals(earliest));
        }
    }
}
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace DictStreamProvider
{
    public class DictQueueAdapter : IQueueAdapter
    {
        //private Queue<string> _queue = new Queue<string>();
        private IStreamQueueMapper _streamQueueMapper;
        private readonly ConcurrentDictionary<QueueId, Queue<byte[]>> _queues = new ConcurrentDictionary<QueueId, Queue<byte[]>>();
        private readonly Logger _logger;

        public DictQueueAdapter(Logger logger, IStreamQueueMapper streamQueueMapper, string name)
        {
            _logger = logger;
            _streamQueueMapper = streamQueueMapper;
            _queues = new ConcurrentDictionary<QueueId, Queue<byte[]>>();

            Name = name;
        }

        public Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token,
            Dictionary<string, object> requestContext)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events), "Trying to QueueMessageBatchAsync null data.");
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (!(token is DictStreamToken))
                throw new ArgumentOutOfRangeException("Token must be of type DictStreamToken");

            var typedToken = token as DictStreamToken;

            // Get the queue
            var queueId = _streamQueueMapper.GetQueueForStream(streamGuid, streamNamespace);
            Queue<byte[]> queue;
            if (!_queues.TryGetValue(queueId, out queue))
            {
                var tmpQueue = new Queue<byte[]>();
                queue = _queues.GetOrAdd(queueId, tmpQueue);
            }

            var eventsAsObjects = events.Cast<object>().ToList();

            if (eventsAsObjects.Count != typedToken.Keys.Length)
                // TODO: What type of exception is this?
                throw new Exception($"Number of keys in token {typedToken.Keys.Length} is not equal to number of events {eventsAsObjects.Count}.");

            var container = new DictBatchContainer(typedToken, streamGuid, streamNamespace, eventsAsObjects, requestContext);

            var bytes = SerializationManager.SerializeToByteArray(container);
            queue.Enqueue(bytes);

            return TaskDone.Done;
        }

        public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
        {
            Queue<byte[]> queue;
            if (!_queues.TryGetValue(queueId, out queue))
            {
                var tmpQueue = new Queue<byte[]>();
                queue = _queues.GetOrAdd(queueId, tmpQueue);
            }

            return new DictQueueAdapterReceiver(_logger, queueId, queue);
        }

        public string Name { get; } = "DictQueueAdapter";
        public bool IsRewindable { get; private set; } = true;

        public StreamProviderDirection Direction
        {
            get
            {
                return StreamProviderDirection.ReadWrite;
            }
        }
    }
}
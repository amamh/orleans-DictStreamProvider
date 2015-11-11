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

        public DictQueueAdapter(IStreamQueueMapper streamQueueMapper, string name)
        {
            _streamQueueMapper = streamQueueMapper;
            Name = name;
        }

        public Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token,
            Dictionary<string, object> requestContext)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events), "Trying to QueueMessageBatchAsync null data.");
            }

            var queueId = _streamQueueMapper.GetQueueForStream(streamGuid, streamNamespace);
            Queue<byte[]> queue;
            if (!_queues.TryGetValue(queueId, out queue))
            {
                var tmpQueue = new Queue<byte[]>();
                queue = _queues.GetOrAdd(queueId, tmpQueue);
            }

            var eventsAsObjects = events.Cast<object>().ToList();

            var container = new SimpleBatchContainer(streamGuid, streamNamespace, eventsAsObjects, requestContext);

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

            return new DictQueueAdapterReceiver(queue);
        }

        public string Name { get; private set; }
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
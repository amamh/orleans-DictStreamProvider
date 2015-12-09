using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Providers;
using Orleans.Streams;
using StackExchange.Redis;
using Orleans.Runtime;
using Orleans;
using System.Collections.Concurrent;

namespace DictStreamProvider.PhysicalQueues.Memory
{
    public class MemoryQueueProvider : IProviderQueue
    {
        private ConcurrentDictionary<QueueId, Queue<byte[]>> _queues;
        private Logger _logger;

        public byte[] Dequeue(QueueId queueId)
        {
            var queue = GetQueue(queueId);
            if (queue.Count == 0)
            {
                _logger.AutoWarn("Trying to dequeue when the queue is empty. This shouldn't happen. Returning null.");
                return null;
            }

            return queue.Dequeue();
        }

        public void Enqueue(QueueId queueId, byte[] bytes)
        {
            var queue = GetQueue(queueId);
            queue.Enqueue(bytes);
        }

        public void Init(Logger logger, IProviderConfiguration config, string providerName, int numQueues)
        {
            _logger = logger;
            _queues = new ConcurrentDictionary<QueueId, Queue<byte[]>>();
        }

        public long Length(QueueId queueId)
        {
            var queue = GetQueue(queueId);
            return queue.Count;
        }

        private Queue<byte[]> GetQueue(QueueId queueId)
        {
            Queue<byte[]> queue;
            if (!_queues.TryGetValue(queueId, out queue))
            {
                var tmpQueue = new Queue<byte[]>();
                queue = _queues.GetOrAdd(queueId, tmpQueue);
            }
            return queue;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers.Streams.Common;
using Orleans.Serialization;
using Orleans.Streams;
using Orleans.Runtime;

namespace DictStreamProvider
{
    public class DictQueueAdapterReceiver : IQueueAdapterReceiver
    {
        public QueueId Id { get; }
        private readonly Queue<byte[]> _queue;
        private readonly Logger _logger;

        public DictQueueAdapterReceiver(Logger logger, QueueId queueid, Queue<byte[]> queue)
        {
            _logger = logger;
            Id = queueid;
            _queue = queue;
        }

        public Task Initialize(TimeSpan timeout)
        {
            //throw new NotImplementedException();
            return TaskDone.Done;
        }

        public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
        {
            var listOfMessages = new List<byte[]>();
            for (int i = 0; i < maxCount; i++)
                if (_queue.Count > 0)
                    listOfMessages.Add(_queue.Dequeue());

            var list = (from m in listOfMessages select SerializationManager.DeserializeFromByteArray<DictBatchContainer>(m));
            var dictQueueAdapterBatchContainers = list as IList<DictBatchContainer> ?? list.ToList();

            return Task.FromResult<IList<IBatchContainer>>(dictQueueAdapterBatchContainers.ToList<IBatchContainer>());
        }

        public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
        {
            //Console.WriteLine($"Delivered {messages.Count}, last one has token {messages.Last().SequenceToken}");
            return TaskDone.Done;
        }

        public Task Shutdown(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }
}
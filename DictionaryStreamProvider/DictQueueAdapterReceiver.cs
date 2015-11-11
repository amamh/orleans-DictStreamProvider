using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers.Streams.Common;
using Orleans.Serialization;
using Orleans.Streams;

namespace DictStreamProvider
{
    public class DictQueueAdapterReceiver : IQueueAdapterReceiver
    {
        public QueueId Id { get; }
        private readonly Queue<byte[]> _queue;
        private long _sequenceId;
        public DictQueueAdapterReceiver(Queue<byte[]> queue)
        {
            //_messages = queue;
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

            var list = (from m in listOfMessages select SerializationManager.DeserializeFromByteArray<SimpleBatchContainer>(m));
            var pipeQueueAdapterBatchContainers = list as IList<SimpleBatchContainer> ?? list.ToList();
            foreach (var batchContainer in pipeQueueAdapterBatchContainers)
                batchContainer.EventSequenceToken = new EventSequenceToken(_sequenceId++);

            return Task.FromResult<IList<IBatchContainer>>(pipeQueueAdapterBatchContainers.ToList<IBatchContainer>());
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
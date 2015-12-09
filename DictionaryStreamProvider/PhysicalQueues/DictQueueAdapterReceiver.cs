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

namespace DictStreamProvider.PhysicalQueues
{
    public class DictQueueAdapterReceiver : IQueueAdapterReceiver
    {
        public QueueId Id { get; }
        private readonly Queue<byte[]> _queue;
        private readonly Logger _logger;
        private readonly IProviderQueue _queueProvider;

        public DictQueueAdapterReceiver(Logger logger, QueueId queueid, IProviderQueue queueProvider)
        {
            _logger = logger;
            _queueProvider = queueProvider;
            Id = queueid;
        }

        public Task Initialize(TimeSpan timeout)
        {
            //throw new NotImplementedException();
            return TaskDone.Done;
        }

        public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
        {
            var listOfMessages = new List<byte[]>();

            var listLength = _queueProvider.Length(Id);
            var max = Math.Min(maxCount, listLength);

            for (var i = 0; i < max; i++)
            {
                var nextMsg = _queueProvider.Dequeue(Id);
                if (nextMsg != null)
                    listOfMessages.Add(nextMsg);
                else
                    _logger.AutoWarn("The queue returned a null message. This shouldn't happen. Ignored.");
            }

            var list = (from m in listOfMessages select SerializationManager.DeserializeFromByteArray<DictBatchContainer>(m));
            var dictQueueAdapterBatchContainers = list as IList<DictBatchContainer> ?? list.ToList();

            return Task.FromResult<IList<IBatchContainer>>(dictQueueAdapterBatchContainers.ToList<IBatchContainer>());
        }

        public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
        {
            var count = messages == null ? 0 : messages.Count;
            if (count == 0)
                return TaskDone.Done;
            var lastToken = messages?.Count != 0 ? messages?.Last()?.SequenceToken.ToString() : "--";
            _logger.AutoVerbose($"Delivered {count}, last one has token {lastToken}");
            return TaskDone.Done;
        }

        public Task Shutdown(TimeSpan timeout)
        {
            _logger.AutoInfo("Receiver requested to shutdown.");
            return TaskDone.Done;
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;
using StackExchange.Redis;

namespace DictStreamProvider
{
    public class DictQueueAdapterReceiver : IQueueAdapterReceiver
    {
        private readonly Logger _logger;
        private readonly IDatabase _database;
        private readonly string _redisListName;
        public QueueId Id { get; }
        private long _sequenceId;


        public DictQueueAdapterReceiver(Logger logger, QueueId queueid, IDatabase database, string redisListName)
        {
            _logger = logger;
            _database = database;
            _redisListName = redisListName;

            Id = queueid;
        }

        public Task Initialize(TimeSpan timeout)
        {
            return TaskDone.Done;
        }

        public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
        {
            if (!_database.KeyExists(_redisListName))
                return Task.FromResult<IList<IBatchContainer>>(new List<IBatchContainer>());

            var listOfMessages = new List<byte[]>();

            var listLength = _database.ListLength(_redisListName);
            var max = Math.Max(maxCount, listLength);

            for (var i = 0; i < max; i++)
            {
                try
                {
                    var nextMsg = _database.ListRightPop(_redisListName);
                    if (!nextMsg.IsNull)
                        listOfMessages.Add(nextMsg);
                }
                catch (Exception exception)
                {
                    _logger.AutoError($"Couldn't read from Redis list {_redisListName}, exception: {exception}");
                    break;
                }
            }

            var list = (from m in listOfMessages select SerializationManager.DeserializeFromByteArray<SimpleBatchContainer>(m));
            var dictQueueAdapterBatchContainers = list as IList<SimpleBatchContainer> ?? list.ToList();
            foreach (var batchContainer in dictQueueAdapterBatchContainers)
                batchContainer.EventSequenceToken = new EventSequenceToken(_sequenceId++);

            _logger.AutoVerbose($"Read {dictQueueAdapterBatchContainers.Count} batch containers");
            return Task.FromResult<IList<IBatchContainer>>(dictQueueAdapterBatchContainers.ToList<IBatchContainer>());
        }

        public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
        {
            _logger.AutoVerbose($"Delivered {messages.Count}, last one has token {messages.Last().SequenceToken}");
            return TaskDone.Done;
        }

        public Task Shutdown(TimeSpan timeout)
        {
            Console.WriteLine("Receiver shutting");
            return TaskDone.Done;
        }
    }
}
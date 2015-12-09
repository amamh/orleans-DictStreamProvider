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

namespace PipeStreamProvider.PhysicalQueues.Redis
{
    public class RedisQueueAdapterReceiver : IQueueAdapterReceiver
    {
        private readonly Logger _logger;
        private readonly IDatabase _database;
        private readonly string _redisListName;
        public QueueId Id { get; }
        private long _sequenceId;


        public RedisQueueAdapterReceiver(Logger logger, QueueId queueid, IDatabase database, string redisListName)
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

            var list = (from m in listOfMessages select SerializationManager.DeserializeFromByteArray<PipeQueueAdapterBatchContainer>(m));
            var pipeQueueAdapterBatchContainers = list as IList<PipeQueueAdapterBatchContainer> ?? list.ToList();
            foreach (var batchContainer in pipeQueueAdapterBatchContainers)
                batchContainer.SimpleSequenceToken = new SimpleSequenceToken(_sequenceId++);

            _logger.AutoVerbose($"Read {pipeQueueAdapterBatchContainers.Count} batch containers");
            return Task.FromResult<IList<IBatchContainer>>(pipeQueueAdapterBatchContainers.ToList<IBatchContainer>());
        }

        public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
        {
            var count = messages == null ? 0 : messages.Count;
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

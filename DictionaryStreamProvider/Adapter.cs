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
using StackExchange.Redis;

namespace PipeStreamProvider.PhysicalQueues.Redis
{
    public class RedisQueueAdapter : IQueueAdapter
    {
        private Logger _logger;
        private IStreamQueueMapper _streamQueueMapper;
        private ConnectionMultiplexer _connection;
        //private readonly int _databaseNum;
        //private readonly string _server;
        private IDatabase _database;
        private string _redisListBaseName = "OrleansQueue";

        public RedisQueueAdapter(Logger logger, IStreamQueueMapper streamQueueMapper, string name, string server, int database)
        {
            // I'm responsible for the connection
            _connection = ConnectionMultiplexer.ConnectAsync(server).Result;
            _database = _connection.GetDatabase(database);
            logger.AutoInfo($"connection to Redis successful.");

            Init(logger, streamQueueMapper, name, _database);
        }

        public RedisQueueAdapter(Logger logger, IStreamQueueMapper streamQueueMapper, string name, IDatabase db)
        {
            Init(logger, streamQueueMapper, name, db);
        }

        void Init(Logger logger, IStreamQueueMapper streamQueueMapper, string name, IDatabase db)
        {
            _logger = logger;
            _streamQueueMapper = streamQueueMapper;

            _database = db;

            Name = name;

            _redisListBaseName = $"{Environment.MachineName}-{DateTime.UtcNow}-orleans-{Name}-queue";
        }

        public Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token,
            Dictionary<string, object> requestContext)
        {
            if (_database == null)
            {
                _logger.AutoWarn($"Trying to write before connection is made to Redis. This batch of data was ignored.");
                return TaskDone.Done;
            }

            if (events == null)
            {
                throw new ArgumentNullException(nameof(events), "Trying to QueueMessageBatchAsync null data.");
            }

            var queueId = _streamQueueMapper.GetQueueForStream(streamGuid, streamNamespace);
            var redisListName = GetRedisListName(queueId);

            var eventsAsObjects = events.Cast<object>().ToList();

            var container = new PipeQueueAdapterBatchContainer(streamGuid, streamNamespace, eventsAsObjects, requestContext);

            var bytes = SerializationManager.SerializeToByteArray(container);

            try
            {
                _database.ListLeftPush(redisListName, bytes);
            }
            catch (Exception exception)
            {
                _logger.AutoError($"failed to write to Redis list {redisListName}. Exception: {exception}");
            }

            return TaskDone.Done;
        }

        private string GetRedisListName(QueueId queueId)
        {
            return $"{_redisListBaseName}-{queueId}";
        }

        public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
        {
            return new RedisQueueAdapterReceiver(_logger, queueId, _database, GetRedisListName(queueId));
        }

        public string Name { get; private set; }
        public bool IsRewindable { get; } = true;

        public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;
    }
}

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

namespace DictStreamProvider.PhysicalQueues.Redis
{
    // TODO: This really should be in a separate project
    public class RedisQueueProvider : IProviderQueue
    {
        private const string ServerParam = "Server";
        private const string DefaultServer = "localhost:6379";
        private string _server;

        private const string RedisDbParam = "RedisDb";
        private const int DefaultRedisDb = -1;
        private int _databaseNum;

        private ConnectionMultiplexer _connection;
        private IDatabase _database;
        private Logger _logger;
        private string _redisListBaseName;

        public byte[] Dequeue(QueueId queueId)
        {
            var redisListName = GetRedisListName(queueId);
            return _database.ListRightPop(redisListName);
        }

        public void Enqueue(QueueId queueId, byte[] bytes)
        {
            var redisListName = GetRedisListName(queueId);
            try
            {
                _database.ListLeftPush(redisListName, bytes);
            }
            catch (Exception exception)
            {
                _logger.AutoError($"failed to write to Redis list.\n Queue Id: {queueId}\nList name: {redisListName}\nException: {exception}");
            }
        }

        public void Init(Logger logger, IProviderConfiguration config, string providerName, int numQueues)
        {
            _logger = logger;
            _redisListBaseName = $"orleans-{providerName}-queue";
            ReadRedisConnectionParams(config);
            _connection = ConnectionMultiplexer.ConnectAsync(_server).Result;
            _database = _connection.GetDatabase(_databaseNum);
            logger.AutoInfo($"connection to Redis successful.");
        }

        public long Length(QueueId id)
        {
            var redisListName = GetRedisListName(id);
            return _database.ListLength(redisListName);
        }
        private void ReadRedisConnectionParams(IProviderConfiguration config)
        {
            // server
            string server;
            _server = DefaultServer;
            if (config.Properties.TryGetValue(ServerParam, out server))
            {
                if (server == "")
                    throw new ArgumentException($"{DefaultServer} invalid. Must not be empty");
                _server = server;
            }

            // db
            string dbNum;
            _databaseNum = DefaultRedisDb;
            if (config.Properties.TryGetValue(RedisDbParam, out dbNum))
            {
                if (!int.TryParse(dbNum, out _databaseNum))
                    throw new ArgumentException($"{RedisDbParam} invalid.  Must be int");
                if (_databaseNum > 15 || _databaseNum < 0)
                    throw new ArgumentException($"{RedisDbParam} invalid.  Must be from 0 to 15");
            }
        }

        private string GetRedisListName(QueueId queueId)
        {
            return $"{_redisListBaseName}-{queueId}";
        }
    }
}

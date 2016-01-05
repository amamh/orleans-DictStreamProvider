using System;
using System.Threading.Tasks;
using Orleans.Providers;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;
using StackExchange.Redis;

namespace DictStreamProvider
{
    public class DictQueueAdapterFactory : IQueueAdapterFactory
    {
        private const string CacheSizeParam = "CacheSize";
        private const int DefaultCacheSize = 4096;
        private int _cacheSize;

        private const string NumQueuesParam = "NumQueues";
        private const int DefaultNumQueues = 8; // keep as power of 2.
        private int _numQueues;

        private const string ServerParam = "Server";
        private const string DefaultServer = "localhost:6379";
        private string _server;

        private const string RedisDbParam = "RedisDb";
        private const int DefaultRedisDb = -1;
        private int _databaseNum;

        private const string UseRedisForCacheParam = "UseRedisForCache";
        private const bool DefaultUseRedisForCache = false;
        private bool _useRedisForCache;

        // TODO: This should be an enum to choose which physical queue to use
        private const string UseRedisForQueueParam = "UseRedisForQueue";
        private const bool DefaultUseRedisForQueue = true;
        private bool _useRedisForQueue;

        private string _providerName;
        private Logger _logger;
        private HashRingBasedStreamQueueMapper _streamQueueMapper;
        private IQueueAdapterCache _adapterCache;
        private IConnectionMultiplexer _connection;
        private IDatabase _db;
        private IProviderConfiguration _config;

        public void Init(IProviderConfiguration config, string providerName, Logger logger)
        {
            _logger = logger;
            _config = config;
            _providerName = providerName;

            // TODO: Do we need this? We want to cache everything, we don't want to trim.
            // Cache size
            string cacheSizeString;
            _cacheSize = DefaultCacheSize;
            if (config.Properties.TryGetValue(CacheSizeParam, out cacheSizeString))
            {
                if (!int.TryParse(cacheSizeString, out _cacheSize))
                    throw new ArgumentException($"{CacheSizeParam} invalid.  Must be int");
            }

            // # queues
            string numQueuesString;
            _numQueues = DefaultNumQueues;
            if (config.Properties.TryGetValue(NumQueuesParam, out numQueuesString))
            {
                if (!int.TryParse(numQueuesString, out _numQueues))
                    throw new ArgumentException($"{NumQueuesParam} invalid.  Must be int");
            }

            // Use Redis for cache?
            string useRedisCache;
            _useRedisForCache = DefaultUseRedisForCache;
            if (config.Properties.TryGetValue(UseRedisForCacheParam, out useRedisCache))
            {
                if (!bool.TryParse(useRedisCache, out _useRedisForCache))
                    throw new ArgumentException($"{UseRedisForCacheParam} invalid value {useRedisCache}");
            }

            // Use Redis for queue?
            string useRedis;
            _useRedisForQueue = DefaultUseRedisForQueue;
            if (config.Properties.TryGetValue(UseRedisForQueueParam, out useRedis))
            {
                if (!bool.TryParse(useRedis, out _useRedisForQueue))
                    throw new ArgumentException($"{UseRedisForQueueParam} invalid value {useRedis}");
            }

            if (_useRedisForCache)
                ReadRedisConnectionParams(config);

            //if (_useRedisForQueue)
            //    // this will be a duplicate step if we are using redis for cache, but it's better to separate the logic
            //    ReadRedisConnectionParams(config);

            _streamQueueMapper = new HashRingBasedStreamQueueMapper(_numQueues, providerName);
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


        public async Task<IQueueAdapter> CreateAdapter()
        {
            // In AzureQueueAdapterFactory an adapter is made per call, so we do the same
            IQueueAdapter adapter;
            if (_useRedisForQueue)
            {
                //adapter = new PhysicalQueues.Redis.RedisQueueAdapter(_logger, GetStreamQueueMapper(), _providerName, _server, _databaseNum);
                var redisQueueProvider = new PhysicalQueues.Redis.RedisQueueProvider();
                var redisAdapter = new PhysicalQueues.DictQueueAdapter(_logger, GetStreamQueueMapper(), _providerName, _config, redisQueueProvider, _numQueues);
                await redisAdapter.Init();
                adapter = redisAdapter;
            }
            else
            {
                var memoryQueueProvider = new PhysicalQueues.Memory.MemoryQueueProvider();
                adapter = new PhysicalQueues.DictQueueAdapter(_logger, GetStreamQueueMapper(), _providerName, _config, memoryQueueProvider, _numQueues);
            }

            return adapter;
        }

        // In AzureQueueAdapterFactory an adapter is made per instance, so we do the same
        public IQueueAdapterCache GetQueueAdapterCache()
        {
            if (_useRedisForCache)
            {
                MakeSureRedisConnected();
                return _adapterCache ?? (_adapterCache = new Cache.Redis.QueueAdapterCacheRedis(_logger, _db));
            }

            return _adapterCache ?? (_adapterCache = new Cache.Memory.DictQueueAdapterCache(this, _cacheSize, _logger));
        }

        public IStreamQueueMapper GetStreamQueueMapper()
        {
            return _streamQueueMapper;
        }

        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
        {
            return Task.FromResult<IStreamFailureHandler>(new LoggerStreamFailureHandler(_logger));
        }

        private void MakeSureRedisConnected()
        {
            if (_connection?.IsConnected == true)
                return;

            // Note: using non-async Connect doesn't work
            _connection = ConnectionMultiplexer.ConnectAsync(_server).Result;
            _db = _connection.GetDatabase(_databaseNum);
        }
    }
}
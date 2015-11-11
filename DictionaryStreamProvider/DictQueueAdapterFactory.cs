using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace DictStreamProvider
{
    public class DictQueueAdapterFactory : IQueueAdapterFactory
    {
        private const string CACHE_SIZE_PARAM = "CacheSize";
        private const int DEFAULT_CACHE_SIZE = 4096;
        private const string NUM_QUEUES_PARAM = "NumQueues";

        public const int DEFAULT_NUM_QUEUES = 8; // keep as power of 2.

        private string _providerName;
        private Logger _logger;
        private int _cacheSize;
        private int _numQueues;
        private HashRingBasedStreamQueueMapper _streamQueueMapper;
        private IQueueAdapterCache _adapterCache;

        public void Init(IProviderConfiguration config, string providerName, Logger logger)
        {
            _logger = logger;
            _providerName = providerName;

            // Cache size
            string cacheSizeString;
            _cacheSize = DEFAULT_CACHE_SIZE;
            if (config.Properties.TryGetValue(CACHE_SIZE_PARAM, out cacheSizeString))
            {
                if (!int.TryParse(cacheSizeString, out _cacheSize))
                    throw new ArgumentException(String.Format("{0} invalid.  Must be int", CACHE_SIZE_PARAM));
            }

            // # queues
            string numQueuesString;
            _numQueues = DEFAULT_NUM_QUEUES;
            if (config.Properties.TryGetValue(NUM_QUEUES_PARAM, out numQueuesString))
            {
                if (!int.TryParse(numQueuesString, out _numQueues))
                    throw new ArgumentException(String.Format("{0} invalid.  Must be int", NUM_QUEUES_PARAM));
            }


            _streamQueueMapper = new HashRingBasedStreamQueueMapper(_numQueues, providerName);
            _adapterCache = new DictQueueAdapterCache(this, _cacheSize, _logger);
        }

        public Task<IQueueAdapter> CreateAdapter()
        {
            var adapter = new DictQueueAdapter(GetStreamQueueMapper(), _providerName);
            return Task.FromResult<IQueueAdapter>(adapter);
        }

        public IQueueAdapterCache GetQueueAdapterCache()
        {
            return _adapterCache;
        }

        public IStreamQueueMapper GetStreamQueueMapper()
        {
            return _streamQueueMapper;
        }

        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
        {
            return Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler(false));
        }
    }
}
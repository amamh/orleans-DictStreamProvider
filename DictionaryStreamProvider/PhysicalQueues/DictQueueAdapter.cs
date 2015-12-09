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
using Orleans.Providers;

namespace DictStreamProvider.PhysicalQueues
{
    public class DictQueueAdapter : IQueueAdapter
    {
        //private Queue<string> _queue = new Queue<string>();
        private IStreamQueueMapper _streamQueueMapper;
        private readonly Logger _logger;
        private readonly IProviderQueue _queueProvider;

        public DictQueueAdapter(Logger logger, IStreamQueueMapper streamQueueMapper, string providerName, IProviderConfiguration config, IProviderQueue queueProvider, int numOfQueues)
        {
            Name = providerName; // WTF: If you set the name to anything else, the client won't receive any messages !?????
            _logger = logger;
            _streamQueueMapper = streamQueueMapper;
            _queueProvider = queueProvider;

            _queueProvider.Init(_logger, config, providerName, numOfQueues);
        }

        public Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token,
            Dictionary<string, object> requestContext)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events), "Trying to QueueMessageBatchAsync null data.");
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (!(token is DictStreamToken))
                throw new ArgumentOutOfRangeException("Token must be of type DictStreamToken");

            var typedToken = token as DictStreamToken;

            var queueId = _streamQueueMapper.GetQueueForStream(streamGuid, streamNamespace);

            var eventsAsObjects = events.Cast<object>().ToList();

            if (eventsAsObjects.Count != typedToken.Keys.Length)
                throw new DictionaryStreamException($"Number of keys in token {typedToken.Keys.Length} is not equal to number of events {eventsAsObjects.Count}.");

            var container = new DictBatchContainer(typedToken, streamGuid, streamNamespace, eventsAsObjects, requestContext);

            var bytes = SerializationManager.SerializeToByteArray(container);

            _queueProvider.Enqueue(queueId, bytes);

            return TaskDone.Done;
        }

        public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
        {
            return new DictQueueAdapterReceiver(_logger, queueId, _queueProvider);
        }

        public string Name { get; }
        public bool IsRewindable { get; } = true;
        public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;
    }
}
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictStreamProvider
{
    [Serializable]
    /// <summary>
    /// A BatchContainer that uses DictSequenceToken
    /// </summary>
    public class DictBatchContainer : IBatchContainer
    {
        public int EventsCount { get { return Events.Count; } }
        public List<object> Events { get; private set; }

        private readonly Dictionary<string, object> _requestContext;
        private List<DictBatchContainer> _batchPerEvent;

        public DictStreamToken TypedSequenceToken { get; }
        public StreamSequenceToken SequenceToken { get { return TypedSequenceToken; } }
        public Guid StreamGuid { get; }
        public string StreamNamespace { get; }
        
        /// <summary>
        /// Split this batch into smaller ones each containing one event and a token that contains only one key
        /// </summary>
        public List<DictBatchContainer> BatchPerEvent
        {
            get
            {
                return _batchPerEvent ?? (_batchPerEvent = SplitIntoSmallBatches());
            }
        }

        public DictBatchContainer(DictStreamToken token, Guid streamGuid, string streamNamespace, List<object> events, Dictionary<string, object> requestContext)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events), "Message contains no events");
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (token.Keys.Length != events.Count)
                throw new DictionaryStreamException("Number of keys in the token must equal the number of events");

            StreamGuid = streamGuid;
            StreamNamespace = streamNamespace;
            Events = events;
            _requestContext = requestContext;
            TypedSequenceToken = token;
        }

        private List<DictBatchContainer> SplitIntoSmallBatches()
        {
            var list = new List<DictBatchContainer>(EventsCount);
            for (int i = 0; i < EventsCount; i++)
                list.Add(new DictBatchContainer(TypedSequenceToken.CreateTokenForKey(i), StreamGuid, StreamNamespace, new List<object>() { Events[i] }, _requestContext));

            return list;
        }

        /// <summary>
        /// For each event, there will be a new token
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
        {
            // TODO: Cache this. Or is it cached automatically?
            for (int i = 0; i < Events.Count; i++)
            {
                if (Events[i] is T)
                {
                    yield return Tuple.Create<T, StreamSequenceToken>((T)Events[i], TypedSequenceToken.CreateTokenForKey (i));
                }
            }
        }

        public bool ImportRequestContext()
        {
            if (_requestContext == null)
                return false;
            RequestContext.Import(_requestContext);
            return true;
        }

        public bool ShouldDeliver(IStreamIdentity stream, object filterData, StreamFilterPredicate shouldReceiveFunc)
        {
            foreach (object item in Events)
            {
                if (shouldReceiveFunc(stream, filterData, item))
                    return true; // There is something in this batch that the consumer is intereted in, so we should send it.
            }
            return false; // Consumer is not interested in any of these events, so don't send.
        }

        public override string ToString()
        {
            return $"[DictBatchContainer:Stream={StreamGuid},#Items={Events.Count}]";
        }
    }
}

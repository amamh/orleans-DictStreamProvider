using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictStreamProvider.PhysicalQueues
{
    public interface IProviderQueue
    {
        bool IsInitialised { get; }
        Task Init(Logger logger, IProviderConfiguration config, string providerName, int numQueues);
        Task Enqueue(QueueId queueId, byte[] batch);
        Task<byte[]> Dequeue(QueueId queueId);
        Task<long> Length(QueueId id);
    }
}

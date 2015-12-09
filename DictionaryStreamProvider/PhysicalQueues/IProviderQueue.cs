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
        void Init(Logger logger, IProviderConfiguration config, string providerName, int numQueues);
        void Enqueue(QueueId queueId, byte[] batch);
        byte[] Dequeue(QueueId queueId);
        long Length(QueueId queueId);
    }
}

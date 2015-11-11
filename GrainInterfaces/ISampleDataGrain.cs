using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;
using DictStreamProvider;

namespace GrainInterfaces
{
    public interface ISampleDataGrain : IGrainWithIntegerKey
    {
        Task SetRandomData(IObjectWithUniqueId<int> random);
        Task<IAsyncStream<IObjectWithUniqueId<int>>> GetStream();

        //Task<Immutable<LinkedList<TWrapper>>> Subscribe(IAsyncObserver<TWrapper> observer, bool recover);
    }
}

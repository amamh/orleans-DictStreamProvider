using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;
using DictStreamProvider;
using DataTypes;

namespace GrainInterfaces
{
    public interface ISampleDataGrain : IGrainWithIntegerKey
    {
        Task SetRandomData(IObjectWithUniqueId<Price> random);
        Task<IAsyncStream<IObjectWithUniqueId<Price>>> GetStream();

        //Task<Immutable<LinkedList<TWrapper>>> Subscribe(IAsyncObserver<TWrapper> observer, bool recover);
    }
}

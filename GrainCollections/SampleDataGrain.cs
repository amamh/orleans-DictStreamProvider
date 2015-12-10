using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;
using DictStreamProvider;
using DataTypes;

namespace GrainCollections
{
    public class SampleDataGrain : Orleans.Grain, ISampleDataGrain
    {
        private Guid _streamGuid;
        private IAsyncStream<IObjectWithUniqueId<Price>> _stream;
        private LinkedList<IObjectWithUniqueId<Price>> _historicalData;
        private IStreamProvider _streamProvider;
        public const string ProviderToUse = "DSProvider";
        //public const string ProviderToUse = "SMSProvider";
        public const string StreamNamespace = "GlobalNamespace";

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            _historicalData = new LinkedList<IObjectWithUniqueId<Price>>();
            _streamGuid = Guid.NewGuid();
            _streamProvider = GetStreamProvider(ProviderToUse);
            _stream = _streamProvider.GetStream<IObjectWithUniqueId<Price>>(_streamGuid, StreamNamespace);
        }

        public Task SetRandomData(IObjectWithUniqueId<Price> random)
        {
            _historicalData.AddLast(random);
            _stream.OnNextAsync(random, new DictStreamToken(random.Id));
            return TaskDone.Done;
        }

        public Task<IAsyncStream<IObjectWithUniqueId<Price>>> GetStream()
        {
            return Task.FromResult(_stream);
        }

        //public async Task Subscribe(IAsyncObserver<IntWithId> observer)
        //{
        //    await _stream.SubscribeAsync(observer);
        //}
    }
}

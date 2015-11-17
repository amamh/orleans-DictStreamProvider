using DataTypes;
using DictStreamProvider;
using GrainInterfaces;
using Orleans;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class TestObserver : IAsyncObserver<IObjectWithUniqueId<Price>>
    {
        public async Task Subscribe()
        {
            var ccGrain = GrainClient.GrainFactory.GetGrain<ISampleDataGrain>(0);
            var stream = await ccGrain.GetStream();
            await stream.SubscribeAsync(this, new EventSequenceToken(0));
        }
        public Task OnNextAsync(IObjectWithUniqueId<Price> item, StreamSequenceToken token = null)
        {
            Console.WriteLine("{0} : {1}", item.Id, item.Value.p);
            return TaskDone.Done;
        }
        public Task OnCompletedAsync()
        {
            throw new NotImplementedException();
        }

        public Task OnErrorAsync(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}

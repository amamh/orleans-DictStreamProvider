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
            await stream.SubscribeAsync(this);
        }
        public Task OnNextAsync(IObjectWithUniqueId<Price> item, StreamSequenceToken token = null)
        {
            var typedToken = token as DictStreamToken;

            Console.WriteLine($"token first key: {typedToken?.Keys?[0]}, \t\titem: {item.Id} : {item.Value.p}");
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

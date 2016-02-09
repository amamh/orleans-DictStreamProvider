using DataTypes;
using DictStreamProvider;
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
    public class TestObserver : IAsyncObserver<Price>
    {
        public Task OnNextAsync(Price item, StreamSequenceToken token = null)
        {
            var typedToken = token as DictStreamToken;

            Console.WriteLine($"token first key: {typedToken?.Keys?[0]}, \t\titem: {item.p}");
            return TaskDone.Done;
        }
        public Task OnCompletedAsync()
        {
            Console.WriteLine("Done");
            return TaskDone.Done;
        }

        public Task OnErrorAsync(Exception ex)
        {
            Console.WriteLine(ex);
            return TaskDone.Done;
        }
    }
}

using System;
using System.Threading.Tasks;
using GrainCollections;
using GrainInterfaces;
using Orleans;
using Orleans.Streams;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    GrainClient.Initialize();
                    break;
                }
                catch (Exception)
                {
                    Task.Delay(500).Wait();
                }
            }
            Console.WriteLine("Waiting");
            Task.Delay(2000).Wait();
            Console.WriteLine("Starting");
            var testObserver = GrainClient.GrainFactory.GetGrain<ITestObserver>(0);
            //var testObserver = new TestObserver();
            testObserver.Subscribe().Wait();
            Console.ReadLine();
        }

        //public class TestObserver : ITestObserver, IGrainObserver
        //{
        //    public async Task Subscribe()
        //    {
        //        var ccGrain = GrainClient.GrainFactory.GetGrain<ISampleDataGrain>(0);
        //        var stream = await ccGrain.GetStream();
        //        await stream.SubscribeAsync(this, null);
        //    }

        //    public Task OnNextAsync(int item, StreamSequenceToken token = null)
        //    {
        //        Console.WriteLine(item);
        //        return TaskDone.Done;
        //    }

        //    public Task OnCompletedAsync()
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public Task OnErrorAsync(Exception ex)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}
    }
}

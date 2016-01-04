using System;
using System.Threading.Tasks;
using GrainCollections;
using GrainInterfaces;
using Orleans;
using Orleans.Streams;
using DictStreamProvider;
using Orleans.Providers.Streams.Common;

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
            Task.Delay(5000).Wait();
            Console.WriteLine("Starting");
            //var testObserver = GrainClient.GrainFactory.GetGrain<ITestObserver>(0);
            var testObserver = new TestObserver();
            testObserver.Subscribe().Wait();
            Console.ReadLine();
        }
    }
}

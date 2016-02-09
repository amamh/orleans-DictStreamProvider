using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;
using DictStreamProvider;
using Orleans.Providers.Streams.Common;
using DataTypes;

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


            var streamProvider = GrainClient.GetStreamProvider("DSProvider");
            var stream = streamProvider.GetStream<Price>(new Guid("00000000-0000-0000-0000-000000000000"), "Global");

            var testObserver = new TestObserver();
            stream.SubscribeAsync(testObserver).Wait();

            Console.ReadLine();
        }
    }
}

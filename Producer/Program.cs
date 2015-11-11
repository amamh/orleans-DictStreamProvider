using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;
using GrainCollections;

namespace Producer
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
                    Task.Delay(100).Wait();
                }
            }

            for (int i = 0; i < 15; i++)
            {
                var id = i % 10;
                var value = i;

                var grain = GrainClient.GrainFactory.GetGrain<ISampleDataGrain>(0);
                var o = new IntWithId { Id = id.ToString(), Value = value };

                grain.SetRandomData(o).Wait();
                Console.WriteLine($"Writing... {id} : {value}");
            }

            var j = 20;
            while (true)
            {
                var id = 15 + j % 15;
                var value = j;

                var grain = GrainClient.GrainFactory.GetGrain<ISampleDataGrain>(0);
                var o = new IntWithId {Id = id.ToString(), Value = value };

                grain.SetRandomData(o).Wait();
                Console.WriteLine($"Writing... {id} : {value}");

                j++;
                Task.Delay(1000).Wait();
            }
        }
    }
}

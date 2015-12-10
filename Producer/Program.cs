using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;
using GrainCollections;
using DictStreamProvider;
using DataTypes;

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

            var grain = GrainClient.GrainFactory.GetGrain<ISampleDataGrain>(0);
            var stream = grain.GetStream().Result;
            var grain2 = GrainClient.GrainFactory.GetGrain<ISampleDataGrain>(1); // different stream
            var stream2 = grain2.GetStream().Result;

            for (int i = 0; i < 15; i++)
            {
                var id = i % 10;
                var value = new Price { p = i };

                var o = new PriceWithId { Id = id.ToString(), Value = value };

                //grain.SetRandomData(o).Wait();
                stream.OnNextAsync(o, new DictStreamToken(o.Id));
                Console.WriteLine($"1- Writing... {id} : {value.p}");

                o.Value.p += 1;
                //grain2.SetRandomData(o).Wait();
                stream2.OnNextAsync(o, new DictStreamToken(o.Id));
                Console.WriteLine($"2- Writing... {id} : {value.p}");
            }

            var j = 20;
            while (true)
            {
                var id = 15 + j % 15;
                var value = new Price { p = j };

                var o = new PriceWithId {Id = id.ToString(), Value = value };
                //grain.SetRandomData(o).Wait();
                stream.OnNextAsync(o, new DictStreamToken(o.Id));
                Console.WriteLine($"1- Writing... {id} : {value.p}");

                o.Value.p += 1;
                //grain2.SetRandomData(o).Wait();
                stream2.OnNextAsync(o, new DictStreamToken(o.Id));
                Console.WriteLine($"2- Writing... {id} : {value.p}");

                j++;
                Task.Delay(1000).Wait();
            }
        }
    }
}

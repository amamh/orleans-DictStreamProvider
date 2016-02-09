using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
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

            var streamProvider = GrainClient.GetStreamProvider("DSProvider");
            var stream = streamProvider.GetStream<Price>(new Guid("00000000-0000-0000-0000-000000000000"), "Global");
            var stream2 = streamProvider.GetStream<Price>(new Guid("00000000-0000-0000-0000-000000000001"), "Global");

            for (int i = 0; i < 15; i++)
            {
                var id = i % 10;
                var value = new Price { p = i };

                stream.OnNextAsync(value, new DictStreamToken(id.ToString()));
                Console.WriteLine($"1- Writing... {id} : {value.p}");

                value.p += 1;
                stream2.OnNextAsync(value, new DictStreamToken(id.ToString()));
                Console.WriteLine($"2- Writing... {id} : {value.p}");
            }

            var j = 20;
            while (true)
            {
                var id = 15 + j % 15;
                var value = new Price { p = j };

                stream.OnNextAsync(value, new DictStreamToken(id.ToString()));
                Console.WriteLine($"1- Writing... {id} : {value.p}");

                value.p += 1;
                stream2.OnNextAsync(value, new DictStreamToken(id.ToString()));
                Console.WriteLine($"2- Writing... {id} : {value.p}");

                j++;
                Task.Delay(1000).Wait();
            }
        }
    }
}

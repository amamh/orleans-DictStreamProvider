using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;

namespace OrleansServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //AppDomain hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
            //{
            //    AppDomainInitializer = InitSilo,
            //    AppDomainInitializerArguments = args,
            //});

            InitSilo(args);
        }

        private static void InitSilo(string[] args)
        {
            var wrapper = new OrleansHostWrapper(args);
            wrapper.Run();
        }
    }
}

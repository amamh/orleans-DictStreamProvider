using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;
using DictStreamProvider;

namespace GrainInterfaces
{
    /// <summary>
    /// Orleans grain communication interface ITestObserver
    /// </summary>
    public interface ITestObserver : Orleans.IGrainWithIntegerKey, IAsyncObserver<IObjectWithUniqueId<int>>
    {
        Task Subscribe();
    }
}

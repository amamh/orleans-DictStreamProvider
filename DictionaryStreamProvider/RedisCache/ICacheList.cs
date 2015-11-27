using System;
using Orleans.Streams;

namespace DictStreamProvider.RedisCache
{
    public interface IRedisCache<T>
    {
        T Get(DictSequenceToken token);
        bool Put(IBatchContainer batch);
        long Count { get; }
    }
}
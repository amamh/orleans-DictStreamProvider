using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictStreamProvider
{
    public interface IObjectWithUniqueId<T>
    {
        string Id { get; }
        T Value { get; }
    }
}

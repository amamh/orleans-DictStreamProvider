using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictStreamProvider
{
    public interface IObjectWithUniqueId<out T> where T : class
    {
        string Id { get; }
        T Value { get; }
    }
}

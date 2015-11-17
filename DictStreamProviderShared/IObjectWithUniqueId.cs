using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictStreamProvider
{
    [Serializable]
    public class EntityType
    {
    }

    public interface IObjectWithUniqueId<out T> where T : EntityType
    {
        string Id { get; }
        T Value { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DictStreamProvider;

namespace GrainCollections
{
    [Serializable]
    public class IntWithId : IObjectWithUniqueId<int>
    {
        public string Id { get; set; }
        public int Value { get; set; }
    }
}

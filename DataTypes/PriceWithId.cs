using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DictStreamProvider;

namespace DataTypes
{
    [Serializable]
    public class PriceWithId : IObjectWithUniqueId<Price>
    {
        public string Id { get; set; }
        public Price Value { get; set; }
    }
}
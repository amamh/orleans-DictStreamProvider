using DictStreamProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTypes
{
    [Serializable]
    public class Price : EntityType
    {
        public double p { get; set; }
    }
}

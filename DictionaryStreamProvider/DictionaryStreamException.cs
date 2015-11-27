using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictStreamProvider
{
    public class DictionaryStreamException : Exception
    {
        public DictionaryStreamException(string message) : base(message) { }
    }
}

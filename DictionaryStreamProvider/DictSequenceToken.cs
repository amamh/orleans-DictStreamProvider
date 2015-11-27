using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictStreamProvider
{
    /// <summary>
    /// Simply a collection of unique string keys, the order should not matter
    /// </summary>
    [Serializable]
    public class DictSequenceToken : StreamSequenceToken
    {
        public string[] Keys { get; private set; }

        public DictSequenceToken(IEnumerable<string> keys)
        {
            CoreCtor(keys);
        }

        public DictSequenceToken(params string[] keys)
        {
            CoreCtor(keys);
        }

        void CoreCtor(IEnumerable<string> keys) {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            Keys = keys as string[] ?? keys.ToArray();

            if (Keys.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(keys), "You can't have a token with no keys");

            Debug.Assert(NoDups(Keys));
        }

        public DictSequenceToken CreateTokenForKey(int index)
        {
            return new DictSequenceToken(Keys[index]);
        }

        private bool NoDups(string[] keys)
        {
            if (Keys.Length < 2)
                return true;

            // O(nlogn) dup check
            Array.Sort(Keys);
            for (int i = 1; i < Keys.Length; i++)
                if (Keys[i] == Keys[i - 1])
                    return false;

            return true;
        }

        public override int CompareTo(StreamSequenceToken other)
        {
            //if (Equals(other))
            //    return 0;
            return 1;
        }

        public override bool Equals(StreamSequenceToken other)
        {
            if (!(other is DictSequenceToken))
                return false;
            return Equals(other as DictSequenceToken);
        }

        public bool Equals(DictSequenceToken other)
        {
            if (other?.Keys?.Length != Keys.Length)
                return false;
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i].Equals(other.Keys[i]))
                    return false;
            }

            return true;
        }
    }
}

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
    public class DictStreamToken : StreamSequenceToken
    {
        public string[] Keys { get; private set; }

        public bool IsOneKey => Keys.Length == 1;

        public DictStreamToken(IEnumerable<string> keys)
        {
            CoreCtor(keys);
        }

        public DictStreamToken(params string[] keys)
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

        public DictStreamToken CreateTokenForKey(int index)
        {
            return new DictStreamToken(Keys[index]);
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
            if (Equals(other))
                return 0;
            return 1;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DictStreamToken);
        }

        public override bool Equals(StreamSequenceToken other)
        {
            if (!(other is DictStreamToken))
                return false;
            return Equals(other as DictStreamToken);
        }

        public bool Equals(DictStreamToken other)
        {
            if (other?.Keys?.Length != Keys.Length)
                return false;
            for (int i = 0; i < Keys.Length; i++)
            {
                if (!Keys[i].Equals(other.Keys[i]))
                    return false;
            }

            return true;
        }
    }
}

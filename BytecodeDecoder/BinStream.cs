using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytecodeDecoder
{
    internal class BinStream
    {
        private byte[] source;
        private int ptr = 0;
        private Random? random;
        private int seed = 0;

        public BinStream(byte[] source)
        {
            this.source = source;
        }

        public bool Randomized => ptr >= source.Length;

        public byte GetNext()
        {
            if(Randomized) return (byte)(random ??= new(seed)).Next();
            return source[ptr++];
        }

        public void Feed(int b, int mask)
        {
            for(int i = 0; i < 8; i++)
            {
                int bit = 1 << i;
                if((mask & bit) != 0)
                {
                    seed <<= 1;
                    if ((b & bit) != 0) seed |= 1;
                }
            }
        }
    }
}

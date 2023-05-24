using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal struct SegmentReference
    {
        private byte[] array;
        private int offset;

        public SegmentReference(byte[] array, int offset)
        {
            this.array = array;
            this.offset = offset;
        }

        public SegmentReference Shift(int offset) => new(array, this.offset + offset);

        public byte Byte => array[offset];

        public ref byte this[int offset] => ref array[this.offset + offset];
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct Pointer
    {
        [FieldOffset(0)]
        public int segOffset;
        [FieldOffset(4)]
        public int segIndex;
        [FieldOffset(0)]
        public long longValue;

        private int segLength => Segment.Get(segIndex).Length;
        private SegmentReference segPtr => Segment.Get(segIndex).Ptr;
        private bool alive => Segment.Get(segIndex)?.Alive ?? false;

        public string Name => $"mem:{segIndex}:{segOffset}";

        public override string? ToString()
            => Name;

        public Pointer(int segIndex, int segOffset)
        {
            this.longValue = 0;
            this.segIndex = segIndex;
            this.segOffset = segOffset;
        }

        public Pointer(long value)
        {
            segOffset = segIndex = 0;
            longValue = value;
        }

        public SegmentReference? GetAccess(int count)
        {
            return (!alive || segOffset + count > segLength || segOffset < 0)
            ? null : segPtr.Shift(segOffset);
        }

        public Pointer Shift(int offset)
            => new(segIndex, segOffset + offset);
    }
}

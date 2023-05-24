using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal class Segment
    {
        private static List<Segment> segments  = new();
        public static Segment? Get(int index) => (index > 0 && index <= segments.Count) ? segments[index - 1] : null;

        public static int TotalCount = segments.Count;

        public static void DeleteAllSegments()
        {
            segments.Clear();
        }

        public readonly int Index;
        public readonly int Length;
        private byte[] data;
        public readonly SegmentReference Ptr;
        public bool Alive { get; private set; }

        public Segment(int length)
        {
            if (length < 0) throw new Interruption("alloc_negative_size");
            Length = length;
            data = new byte[Length];
            Ptr = new(data, 0);
            Alive = true;
            segments.Add(this);
            Index = segments.Count; // 1-based; 0 is considered invalid because of casts
        }

        public void Free()
        {
            Alive = false;
        }

        public Pointer GetPointer(int offset)
        {
            return new Pointer(Index, offset);
        }
    }
}

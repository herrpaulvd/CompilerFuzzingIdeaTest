using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal class Variable
    {
        public readonly Pointer Location;
        public readonly string Name;
        public readonly int Size;
        private const int maxSize = 8;
        public readonly bool IsPointer;

        public Variable(Pointer location, string name, int size, bool isPointer)
        {
            Location = location;
            Name = name;
            Size = size;
            IsPointer = isPointer;
        }

        private SegmentReference GetAddress(int count)
            => Location.GetAccess(count) ?? throw new Interruption("dead_frame");

        public SegmentReference GetAccess()
            => Location.GetAccess(Size) ?? throw new Interruption("dead_frame");

        public long this[int count]
        {
            get
            {
                unsafe
                {
                    if (count > maxSize) throw new Interruption("too_big_integer");
                    count = Math.Min(count, Size);
                    long result = 0;
                    byte* dst = (byte*)&result;
                    var src = GetAddress(count);
                    for (int i = 0; i < count; i++)
                        dst[i] = src[i];
                    if(count < maxSize)
                    {
                        byte fill = (byte)((src[count - 1] & 0x80) / 0x80 * 0xFF);
                        for (int i = count; i < maxSize; i++)
                            dst[i] = fill;
                    }
                    return result;
                }
            }
            set
            {
                unsafe
                {
                    if (count > maxSize) throw new Interruption("too_big_integer");
                    count = Math.Min(count, Size);
                    byte* src = (byte*)&value;
                    var dst = GetAddress(count);
                    for (int i = 0; i < count; i++)
                        dst[i] = src[i];
                    if (count < Size)
                    {
                        byte fill = (byte)((src[count - 1] & 0x80) / 0x80 * 0xFF);
                        for (int i = count; i < Size; i++)
                            dst[i] = fill;
                    }
                }
            }
        }

        public long LongValue
        {
            get => this[Size];
            set => this[Size] = value;
        }

        public Pointer PtrValue
        {
            get
            {
                unsafe
                {
                    return new Pointer(LongValue);
                }
            }

            set
            {
                unsafe
                {
                    LongValue = *(long*)&value;
                }
            }
        }

        public string ShowValue()
            => (IsPointer ? PtrValue.ToString() : LongValue.ToString()) ?? throw new Interruption("internal_output_error");
    }
}

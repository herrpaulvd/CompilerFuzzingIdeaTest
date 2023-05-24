using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal class VMEnvironment
    {
        public static void Initialize()
        {
            Functions.Clear();
            Segment.DeleteAllSegments();
        }

        public static readonly SortedDictionary<(string, int), Function> Functions = new(); 
    }
}

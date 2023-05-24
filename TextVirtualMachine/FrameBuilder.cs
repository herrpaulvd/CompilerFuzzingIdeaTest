using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal class FrameBuilder
    {
        private readonly List<(string, int, int, bool)> variables = new();
        private int size;

        public FrameBuilder(IList<string> names, IList<int> sizes, IList<bool> ptrs)
        {
            this.size = 0;
            for(int i = 0; i < names.Count; i++)
            {
                var name = names[i];
                var size = sizes[i];
                var isptr = ptrs[i];
                variables.Add((name, size, this.size, isptr));
                this.size += size;
            }
        }

        public Frame MakeFrame() => new(variables, size);
    }
}

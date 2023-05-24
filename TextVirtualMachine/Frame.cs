using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal class Frame : Segment
    {
        private SortedDictionary<string, Variable> variables = new();

        public int IP { get; set; } = 0;

        public Variable GetVariable(string name)
        {
            if (variables.TryGetValue(name, out Variable? v))
                return v ?? throw new Interruption("variable_does_not_exist");
            throw new Interruption("variable_does_not_exist");
        }

        public Frame(IList<(string, int, int, bool)> vs, int size)
            : base(size)
        {
            foreach(var (name, type, offset, isptr) in vs)
                variables.Add(name, new Variable(GetPointer(offset), name, type, isptr));
        }
    }
}

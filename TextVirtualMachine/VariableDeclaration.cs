using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal struct VariableDeclaration
    {
        public string Name;
        public string Type;

        public VariableDeclaration(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}

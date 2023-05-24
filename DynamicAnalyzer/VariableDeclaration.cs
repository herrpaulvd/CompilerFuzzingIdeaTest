using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicAnalyzer
{
    internal struct VariableDeclaration
    {
        public string Type;
        public string Name;

        public int Size => Type switch
        {
            "byte" => 1,
            "short" => 2,
            "int" => 4,
            "long" => 8,
            "ptr" => 8,
            _ => throw new NotImplementedException()
        };

        public VariableDeclaration(string type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}

using CompileLib.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class LocalScope : CompileLib.Semantics.CodeObject
    {
        public LocalScope(LocalScope? parent) : base("", "scope", -1, -1)
        {
            if(parent is not null)
                AddRelation("parent", parent);
        }
    }
}

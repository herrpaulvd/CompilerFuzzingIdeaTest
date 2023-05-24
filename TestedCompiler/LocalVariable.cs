using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class LocalVariable : CompileLib.Semantics.CodeObject
    {
        public readonly TypeExpression VarType;
        public readonly string VMName;

        public LocalVariable(
            TypeExpression type,
            string name,
            LocalScope scope,
            string vmname,
            int line,
            int column)
            : base(name, "var", line, column)
        {
            scope.AddRelation("var", this);
            VarType = type;
            VMName = vmname;
        }
    }
}

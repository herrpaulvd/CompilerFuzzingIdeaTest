using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class DeleteStatement : Statement
    {
        public string Variable { get; }

        public DeleteStatement(
            int line, 
            int column,
            string variable
            ) : base(line, column)
        {
            Variable = variable;
        }

        public override void Compile(CompilationManager cm)
        {
            var found = cm.SemanticNetwork.Search(cm.Scope, "@get-local", Variable);
            if (found.Count == 0)
                throw new CompilationError("Variable not found", Line, Column);
            var lv = (LocalVariable)found[0].Result;
            if (!lv.VarType.IsPointer)
                throw new CompilationError("Cannot delete non-pointer", Line, Column);
            cm.SafeDelete(lv.VMName);
        }
    }
}

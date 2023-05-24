using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class VariableExpression : Expression
    {
        public string Name { get; }

        public VariableExpression(
            int line,
            int column,
            string name
            )
            : base(line, column)
        {
            Name = name;
        }

        private LocalVariable? foundlv = null;
        private LocalVariable FindVariable(CompilationManager cm)
        {
            if (foundlv is not null) return foundlv;
            var found = cm.SemanticNetwork.Search(cm.Scope, "@get-local", Name);
            if (found.Count == 0)
                throw new CompilationError("Variable not found", Line, Column);
            return foundlv = (LocalVariable)found[0].Result;
        }

        public override void CompileRight(CompilationManager cm, string output)
        {
            cm.WriteCode($"mov {FindVariable(cm).VMName} {output}");
        }

        protected override TypeExpression ResolveTypeQuickly(CompilationManager cm)
        {
            return FindVariable(cm).VarType;
        }

        public override void CompileLeft(CompilationManager cm, Expression right, string? extraOutput)
        {
            var rt = right.Type(cm);
            if (!rt.IsAssignableTo(Type(cm)))
                throw new CompilationError("Incompatible types", Line, Column);
            var rv = cm.GetTempVariable(rt.VMType, out int rh);
            right.CompileRight(cm, rv);

            cm.WriteCode($"mov {rv} {FindVariable(cm).VMName}");
            if (extraOutput is not null) cm.WriteCode($"mov {rv} {extraOutput}");
            cm.FreeTempVariable(rh);
        }
    }
}

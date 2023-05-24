using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class IndexExpression : Expression
    {
        public Expression Array { get; }
        public Expression Index { get; }

        public IndexExpression(
            int line,
            int column,
            Expression array,
            Expression index
            )
            : base(line, column)
        {
            Array = array;
            Index = index;
        }

        public override void CompileRight(CompilationManager cm, string output)
        {
            var vptr = cm.GetTempVariable("ptr", out int hptr);
            CalculatePointer(cm, Array, Index, false, vptr);
            if (cm.BugMem && Index is ConstExpression)
                cm.WriteCode($"read {vptr} {output}");
            else
                cm.SafeRead(vptr, output);
            cm.FreeTempVariable(hptr);
        }

        protected override TypeExpression ResolveTypeQuickly(CompilationManager cm)
        {
            var at = Array.Type(cm);
            if (at.IsPointer) return at.Dereference();
            throw new CompilationError("Cannot index integer", Line, Column);
        }

        public override void CompileLeft(CompilationManager cm, Expression right, string? extraOutput)
        {
            var vptr = cm.GetTempVariable("ptr", out int hptr);
            CalculatePointer(cm, Array, Index, false, vptr);

            var rt = right.Type(cm);
            if (!rt.IsAssignableTo(Type(cm)))
                throw new CompilationError("Incompatible types", Line, Column);
            var rv = cm.GetTempVariable(rt.VMType, out int rh);
            right.CompileRight(cm, rv);
            cm.SafeWrite(rv, vptr);
            if (extraOutput is not null) cm.WriteCode($"mov {rv} {extraOutput}");

            cm.FreeTempVariable(rh);
            cm.FreeTempVariable(hptr);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal abstract class Expression : CodeObject
    {
        protected Expression(int line, int column) : base(line, column)
        {
        }

        // OUTPUT is defined by the PARENT
        public abstract void CompileRight(CompilationManager cm, string output);
        // RIGHT is not compiled, compilation time is defined by LEFT
        public virtual void CompileLeft(CompilationManager cm, Expression right, string? extraOutput)
        {
            throw new CompilationError("Invalid lvalue expression", Line, Column);
        }

        private TypeExpression? typeCache = null;
        protected abstract TypeExpression ResolveTypeQuickly(CompilationManager cm);
        public TypeExpression Type(CompilationManager cm)
            => typeCache ??= ResolveTypeQuickly(cm);

        protected void CalculatePointer(CompilationManager cm, Expression ptr, Expression offset, bool minus, string output)
        {
            var ot = offset.Type(cm);
            if (ot.IsPointer)
                throw new CompilationError("Pointer value cannot be an offset for pointer", Line, Column);

            var pt = ptr.Type(cm);
            if (!pt.IsPointer)
                throw new CompilationError("Pointer expected", Line, Column);
            var et = pt.Dereference();
            int etsize = et.Size;

            ptr.CompileRight(cm, output);
            var voffset = cm.GetTempVariable("long", out int hoffset);
            offset.CompileRight(cm, voffset);
            if (etsize > 1)
                cm.WriteCode($"mul {voffset} {etsize} {voffset}");
            var ins = minus ? "sub" : "add";
            cm.WriteCode($"{ins} {output} {voffset} {output}");
            cm.FreeTempVariable(hoffset);
        }
    }
}

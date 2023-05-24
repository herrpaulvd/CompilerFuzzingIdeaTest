using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class NewExpression : Expression
    {
        public TypeExpression ElementType { get; }
        public Expression Count { get; }

        public NewExpression(
            int line,
            int column,
            TypeExpression type,
            Expression count
            )
            : base(line, column)
        {
            ElementType = type;
            Count = count;
        }

        public override void CompileRight(CompilationManager cm, string output)
        {
            var vcount = cm.GetTempVariable("long", out int hcount);
            Count.CompileRight(cm, vcount);
            int etsize = ElementType.Size;
            if (etsize > 1)
                cm.WriteCode($"mul {vcount} {etsize} {vcount}");
            cm.SafeNew(vcount, output);

            cm.FreeTempVariable(hcount);
        }

        protected override TypeExpression ResolveTypeQuickly(CompilationManager cm)
        {
            return ElementType.MakePointer();
        }
    }
}

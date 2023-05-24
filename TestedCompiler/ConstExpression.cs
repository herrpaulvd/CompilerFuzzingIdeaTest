using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class ConstExpression : Expression
    {
        public long Value { get; }

        public ConstExpression(
            int line,
            int column,
            long value
            )
            : base(line, column)
        {
            Value = value;
        }

        public override void CompileRight(CompilationManager cm, string? output)
        {
            cm.WriteCode($"mov {Value} {output}");
        }

        private static TypeExpression TypeLong = new("long");

        protected override TypeExpression ResolveTypeQuickly(CompilationManager cm)
        {
            return TypeLong;
        }
    }
}

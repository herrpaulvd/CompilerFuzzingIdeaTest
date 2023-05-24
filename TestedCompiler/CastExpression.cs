using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class CastExpression : Expression
    {
        public TypeExpression DestinationType { get; }
        public Expression Operand { get; }

        public CastExpression(int line, int column, TypeExpression type, Expression operand)
            : base(line, column)
        {
            DestinationType = type;
            Operand = operand;
        }

        public override void CompileRight(CompilationManager cm, string output)
        {
            Operand.CompileRight(cm, output);
        }

        protected override TypeExpression ResolveTypeQuickly(CompilationManager cm)
        {
            return DestinationType;
        }
    }
}

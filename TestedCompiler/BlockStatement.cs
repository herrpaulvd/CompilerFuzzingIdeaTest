using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class BlockStatement : Statement
    {
        public ReadOnlyCollection<Statement> Statements { get; }

        public BlockStatement(
            int line,
            int column,
            IList<Statement> statements
            )
            : base(line, column)
        {
            Statements = new(statements);
        }

        public override void Compile(CompilationManager cm)
        {
            cm.PushScope();
            foreach(var s in  Statements) s.Compile(cm);
            cm.PopScope();
        }
    }
}

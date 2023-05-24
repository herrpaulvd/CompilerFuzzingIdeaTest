using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public Statement Body { get; }

        public WhileStatement(
            int line,
            int column,
            Expression condition,
            Statement body
            )
            : base(line, column)
        {
            Condition = condition;
            Body = body;
        }

        public override void Compile(CompilationManager cm)
        {
            var Lbegin = cm.DefineLabel();
            var Lend = cm.DefineLabel();

            cm.WriteCode($"label {Lbegin}");
            var vcondition = cm.GetTempVariable("long", out int hcondition);
            Condition.CompileRight(cm, vcondition);
            cm.WriteCode($"not {vcondition} {vcondition}");
            cm.WriteCode($"gotoif {vcondition} {Lend}");
            Body.Compile(cm);
            cm.WriteCode($"goto {Lbegin}");
            cm.WriteCode($"label {Lend}");
        }
    }
}

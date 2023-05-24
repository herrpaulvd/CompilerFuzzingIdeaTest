using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class IfStatement : Statement
    {
        public Expression Condition { get; }
        public Statement IfBranch { get; }
        public Statement? ElseBranch { get; }

        public IfStatement(
            int line,
            int column,
            Expression condition, 
            Statement ifBranch, 
            Statement? elseBranch)
            : base(line, column)
        {
            Condition = condition;
            IfBranch = ifBranch;
            ElseBranch = elseBranch;
        }

        public override void Compile(CompilationManager cm)
        {
            var Lelse = cm.DefineLabel();
            var Lend = ElseBranch is null ? Lelse : cm.DefineLabel();

            var vcondition = cm.GetTempVariable("long", out int hcondition);
            Condition.CompileRight(cm, vcondition);
            cm.WriteCode($"not {vcondition} {vcondition}");
            cm.WriteCode($"gotoif {vcondition} {Lelse}");
            
            cm.PushScope();
            IfBranch.Compile(cm);
            cm.PopScope();

            if(ElseBranch is not null)
            {
                cm.WriteCode($"goto {Lend}");
                cm.WriteCode($"label {Lelse}");
                cm.PushScope();
                ElseBranch.Compile(cm);
                cm.PopScope();
            }

            cm.WriteCode($"label {Lend}");

            cm.FreeTempVariable(hcondition);
        }
    }
}

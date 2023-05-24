using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class UnaryExpression : Expression
    {
        public string Operation { get; }
        public Expression Operand { get; }

        public UnaryExpression(
            int line,
            int column,
            string operation,
            Expression operand
            )
            : base(line, column)
        {
            Operation = operation;
            Operand = operand;
        }

        private const string OPref = "&";
        private const string OPderef = "*";
        private static readonly string[] OPothers = { "!", "~", "-", "+" };

        public override void CompileRight(CompilationManager cm, string output)
        {
            if (Operation == OPref)
            {
                if (Operand is VariableExpression ve)
                {
                    var found = cm.SemanticNetwork.Search(cm.Scope, "@get-local", ve.Name);
                    if (found.Count == 0)
                        throw new CompilationError("Variable not found", Line, Column);
                    cm.WriteCode($"addr {((LocalVariable)found[0].Result).VMName} {output}");
                }
                else if (Operand is IndexExpression ie)
                {
                    CalculatePointer(cm, ie.Array, ie.Index, false, output);
                }
                else if (Operand is UnaryExpression ue && ue.Operation == "*")
                {
                    ue.Operand.CompileRight(cm, output);
                }
                else
                    throw new CompilationError("Invalid entity to get address", Line, Column);
            }
            else if (Operation == OPderef)
            {
                var ot = Operand.Type(cm);
                if (!ot.IsPointer) throw new CompilationError("Pointer expected", Line, Column);
                var vo = cm.GetTempVariable(ot.VMType, out int ho);
                Operand.CompileRight(cm, vo);
                cm.SafeRead(vo, output);
                cm.FreeTempVariable(ho);
            }
            else if (OPothers.Contains(Operation))
            {
                if (Operand.Type(cm).IsPointer && Operation != "!")
                    throw new CompilationError("Integer operand expected", Line, Column);
                Operand.CompileRight(cm, output);
                if (Operation == "-")
                {
                    cm.AddOverflowChecker("0", output, Type(cm), "-");
                    cm.WriteCode($"sub 0 {output} {output}");
                }
                else if (Operation == "!")
                    cm.WriteCode($"not {output} {output}");
                else if(Operation == "~")
                    cm.WriteCode($"lnot {output} {output}");
            }
            else
                throw new CompilationError("Internal error: unknown operation", Line, Column);
        }

        protected override TypeExpression ResolveTypeQuickly(CompilationManager cm)
        {
            if(Operation == OPref) return Operand.Type(cm).MakePointer();
            if(Operation == OPderef)
            {
                if (Operand.Type(cm).IsPointer) return Operand.Type(cm).Dereference();
                throw new CompilationError("Cannot dereference integer", Line, Column);
            }
            return Operand.Type(cm);
        }

        public override void CompileLeft(CompilationManager cm, Expression right, string? extraOutput)
        {
            if (Operation != "*") base.CompileLeft(cm, right, extraOutput);

            var ot = Operand.Type(cm);
            if (!ot.IsPointer) throw new CompilationError("Pointer expected", Line, Column);
            var vo = cm.GetTempVariable(ot.VMType, out int ho);
            Operand.CompileRight(cm, vo);

            var rt = right.Type(cm);
            if (!rt.IsAssignableTo(Type(cm)))
                throw new CompilationError("Incompatible types", Line, Column);
            var rv = cm.GetTempVariable(rt.VMType, out int rh);
            right.CompileRight(cm, rv);

            cm.SafeWrite(rv, vo);
            if (extraOutput is not null) cm.WriteCode($"mov {rv} {extraOutput}");

            cm.FreeTempVariable(ho);
            cm.FreeTempVariable(rh);
        }
    }
}

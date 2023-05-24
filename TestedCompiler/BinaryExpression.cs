using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class BinaryExpression : Expression
    {
        public string Operation { get; }
        public Expression Left { get; }
        public Expression Right { get; }

        public BinaryExpression(
            int line,
            int column,
            string operation,
            Expression left,
            Expression right
            )
            : base(line, column)
        {
            Operation = operation;
            Left = left;
            Right = right;
        }

        private readonly static string[] OPptrarithm = { "+", "-" };
        private readonly static HashSet<string> OPcomparison = new() 
        { "==", "!=", "<", ">", "<=", ">=" };
        private readonly static string[] OPeqneq = { "==", "!=" };
        private static readonly string[] OPandalsoorelse = { "&&", "||" };
        private const string OPassign = "=";
        private static readonly HashSet<string> OPothers = new()
        { "*", "/", "%", "&", "|", "^", ">>", "<<"};

        private static SortedDictionary<string, string> op2ins = new()
        {
            {"+", "add" },
            {"-", "sub" },
            {"*", "mul" },
            {"/", "div" },
            {"%", "mod" },
            {"<", "less" },
            {">", "greater" },
            {"<=", "lesseq" },
            {">=", "greatereq" },
            {"==", "eq" },
            {"!=", "neq" },
            {"&", "land" },
            {"|", "lor" },
            {"^", "lxor" },
            {"<<", "shl" },
            {">>", "shr" }
        };

        //private static readonly TypeExpression TYPEbyte = new("byte");
        //private static readonly TypeExpression TYPEint = new("int");
        private static readonly TypeExpression TYPElong = new("long");

        private enum TypeRequirement
        {
            Int,
            Ptr,
            SameAsL,
            Both,
        };
        

        private void CompileDefault(
            CompilationManager cm, 
            string output,
            TypeRequirement leftReq,
            TypeRequirement rightReq)
        {
            var lt = Left.Type(cm);
            var rt = Right.Type(cm);
            void checkreq(string what, TypeExpression t, TypeRequirement req)
            {
                switch(req)
                {
                    case TypeRequirement.Int:
                        if (t.IsPointer)
                            throw new CompilationError($"{what} must be integer", Line, Column);
                        return;
                    case TypeRequirement.Ptr:
                        if (!t.IsPointer)
                            throw new CompilationError($"{what} must be pointer", Line, Column);
                        return;
                    case TypeRequirement.SameAsL:
                        if (t.IsPointer != lt.IsPointer)
                            throw new CompilationError($"{what} type must be of the same kind as the left's one", Line, Column);
                        return;
                    default:
                        return;
                }
            }
            checkreq("Left", lt, leftReq);
            checkreq("Right", rt, rightReq);
            var lv = cm.GetTempVariable(lt.VMType, out int lh);
            var rv = cm.GetTempVariable(rt.VMType, out int rh);
            Left.CompileRight(cm, lv);
            Right.CompileRight(cm, rv);

            if(!cm.BugOF || Left is not ConstExpression || Right is not ConstExpression)
                cm.AddOverflowChecker(lv, rv, Type(cm), Operation);
            cm.WriteCode($"{op2ins[Operation]} {lv} {rv} {output}");
            cm.FreeTempVariable(lh);
            cm.FreeTempVariable(rh);
        }

        private void CompileShortLogical(CompilationManager cm, string output, bool inverse)
        {
            var LreturnLeft = cm.DefineLabel();
            var LreturnRight = cm.DefineLabel();
            var Lend = cm.DefineLabel();

            var lt = Left.Type(cm);
            var lv = cm.GetTempVariable(lt.VMType, out int lh);
            Left.CompileRight(cm, lv);

            var firstLabel = inverse ? LreturnLeft : LreturnRight;
            cm.WriteCode($"gotoif {lv} {firstLabel}");

            void emitLeft()
            {
                cm.WriteCode($"label {LreturnLeft}");
                cm.WriteCode($"mov {lv} {output}");
            }
            void emitRight()
            {
                cm.WriteCode($"label {LreturnRight}");
                Right.CompileRight(cm, output);
            }
            void emitGotoEnd()
            {
                cm.WriteCode($"goto {Lend}");
            }
            if(inverse) { emitRight(); emitGotoEnd(); emitLeft(); }
            else { emitLeft(); emitGotoEnd(); emitRight(); }
            cm.WriteCode($"label {Lend}");
            cm.FreeTempVariable(lh);
        }

        private void CompileAssign(CompilationManager cm, string? output)
        {
            Left.CompileLeft(cm, Right, output);
        }

        public override void CompileRight(CompilationManager cm, string output)
        {
            if(Operation == "=")
            {
                CompileAssign(cm, output);
                return;
            }

            if (OPptrarithm.Contains(Operation))
            {
                if (Left.Type(cm).IsPointer)
                    CalculatePointer(cm, Left, Right, Operation == "-", output);
                else
                    CompileDefault(cm, output, TypeRequirement.Both, TypeRequirement.Int);
            }
            else if (OPeqneq.Contains(Operation))
                CompileDefault(cm, output, TypeRequirement.Both, TypeRequirement.SameAsL);
            else if (OPcomparison.Contains(Operation) || OPothers.Contains(Operation))
                CompileDefault(cm, output, TypeRequirement.Int, TypeRequirement.Int);
            else if (OPandalsoorelse.Contains(Operation))
                CompileShortLogical(cm, output, Operation == "||");
            else
                throw new CompilationError("Internal error: unknown op", Line, Column);
        }

        private static TypeExpression MaxType(TypeExpression a, TypeExpression b)
        {
            if (a.PointerDepth > 0) return a;
            if (b.PointerDepth > 0) return b;
            return Array.IndexOf(Options.BaseTypes, a.BaseTypeName)
                >= Array.IndexOf(Options.BaseTypes, b.BaseTypeName)
                ? a
                : b;
        }

        protected override TypeExpression ResolveTypeQuickly(CompilationManager cm)
        {
            if (Operation == OPassign) return Left.Type(cm);
            if (OPcomparison.Contains(Operation) || OPandalsoorelse.Contains(Operation))
                return TYPElong;
            return MaxType(Left.Type(cm), Right.Type(cm));
        }
    }
}

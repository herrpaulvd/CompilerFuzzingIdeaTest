using CompileLib.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class Syntax
    {
        [SetTag("program")]
        public static GlobalScope ReadProgram(
            [RequireTags("function")] Function main
            )
        {
            return new(main);
        }

        [SetTag("function")]
        public static Function ReadFunction(
            [Keywords("void")] Parsed<string> type,
            [Keywords("main")] string name,
            [Keywords("(")] string paramOpen,
            [Keywords(")")] string paramClose,
            [RequireTags("statement")] Statement block
            )
        {
            return new(type.Line, type.Column, block);
        }

        [SetTag("statement")]
        public static Statement ReadIfStatement(
            [Keywords("if")] Parsed<string> kwIf,
            [Keywords("(")] string condOpen,
            [RequireTags("expression")] Expression condition,
            [Keywords(")")] string condClose,
            [RequireTags("statement")] Statement ifBranch,
            [Optional(true)][Keywords("else")] string? kwElse,
            [TogetherWith][RequireTags("statement")] Statement? elseBranch
            )
        {
            return new IfStatement(kwIf.Line, kwIf.Column, condition, ifBranch, elseBranch);
        }

        [SetTag("statement")]
        public static Statement ReadWhileStatement(
            [Keywords("while")] Parsed<string> kwWhile,
            [Keywords("(")] string condOpen,
            [RequireTags("expression")] Expression condition,
            [Keywords(")")] string condClose,
            [RequireTags("statement")] Statement body
            )
        {
            return new WhileStatement(kwWhile.Line, kwWhile.Column, condition, body);
        }

        [SetTag("type")]
        public static TypeExpression ReadBaseType(
            [RequireTags("id")] Parsed<string> name
            )
        {
            return new(name.Self ?? throw new NotImplementedException());
        }

        [SetTag("type")]
        public static TypeExpression ReadPointer(
            [RequireTags("type")] Parsed<TypeExpression> baseType,
            [Keywords("*")] string asterisk
            )
        {
            return baseType.Self?.MakePointer() ?? throw new NotImplementedException();
        }

        [SetTag("statement")]
        public static Statement ReadExpressionStatement(
            [Optional(true)][Keywords("var")] Parsed<string>? kwVar,
            [TogetherWith][RequireTags("type")] TypeExpression? type,
            [TogetherWith][RequireTags("id")] string? variableName,
            [TogetherWith][Keywords("=")] string eq,
            [RequireTags("expression")] Expression expression,
            [Keywords(";")] string end
            )
        {
            int line = kwVar?.Line ?? expression.Line;
            int column = kwVar?.Column ?? expression.Column;
            return new ExpressionStatement(line, column, type, variableName, expression);
        }

        [SetTag("statement")]
        public static Statement ReadBlock(
            [Keywords("{")] Parsed<string> brOpen,
            [Many(true)][RequireTags("statement")] Statement[] statements,
            [Keywords("}")] string brClose
            )
        {
            return new BlockStatement(brOpen.Line, brOpen.Column, statements);
        }

        [SetExpressionTag("expression")]
        [UnaryOperation("!", -1)] // lnot
        [UnaryOperation("~", -1)] // not
        [UnaryOperation("-", -1)] // 0 - x
        [UnaryOperation("+", -1)] // x
        [UnaryOperation("*", -1)] // read/write
        [UnaryOperation("&", -1)] // addr
        public static Expression ReadUnaryExpression(
            Parsed<string> operation,
            Expression operand
            )
        {
            return new UnaryExpression(operation.Line, operation.Column, operation.Self ?? throw new NotImplementedException(), operand);
        }

        [SetExpressionTag("expression")]
        [BinaryOperation("*", -2)]
        [BinaryOperation("/", -2)]
        [BinaryOperation("%", -2)]
        [BinaryOperation("+", -3)]
        [BinaryOperation("-", -3)]
        [BinaryOperation("==", -4)]
        [BinaryOperation("!=", -4)]
        [BinaryOperation("<", -4)]
        [BinaryOperation(">", -4)]
        [BinaryOperation(">=", -4)]
        [BinaryOperation("<=", -4)]
        [BinaryOperation("&&", -5)]
        [BinaryOperation("||", -5)]
        [BinaryOperation("&", -5)]
        [BinaryOperation("|", -5)]
        [BinaryOperation("^", -5)]
        [BinaryOperation(">>", -5)]
        [BinaryOperation("<<", -5)]
        [BinaryOperation("=", -6, true)]
        public static Expression? ReadBinaryExpression(
            Expression left,
            Parsed<string> operation,
            Expression right
            )
        {
            return new BinaryExpression(left.Line, left.Column,
                operation.Self ?? throw new NotImplementedException(),
                left, right);
        }

        [SetTag("expression")]
        public static Expression UpExpression(
            [RequireTags("atom-expression")] Expression e
            )
        {
            return e;
        }

        [SetTag("atom-expression")]
        public static Expression ReadExpessionInBrackets(
            [Keywords("(")] Parsed<string> brOpen,
            [RequireTags("expression")] Expression expression,
            [Keywords(")")] string brClose
            )
        {
            return expression.ReplaceXY(brOpen.Line, brOpen.Column) as Expression ?? throw new NotImplementedException();
        }

        [SetTag("atom-expression")]
        public static Expression ReadIndex(
            [RequireTags("atom-expression")] Expression array,
            [Keywords("[")] string brOpen,
            [RequireTags("expression")] Expression index,
            [Keywords("]")] string brClose
            )
        {
            return new IndexExpression(array.Line, array.Column, array, index);
        }

        [SetTag("atom-expression")]
        public static Expression ReadVariable(
            [RequireTags("id")] Parsed<string> id
            )
        {
            return new VariableExpression(id.Line, id.Column, id.Self ?? throw new NotImplementedException());
        }

        [SetTag("atom-expression")]
        public static Expression ReadConst(
            [RequireTags("int")] Parsed<string> cnst
            )
        {
            try
            {
                return new ConstExpression(cnst.Line, cnst.Column, long.Parse(cnst.Self));
            }
            catch(Exception)
            {
                throw new CompilationError("Invalid const", cnst.Line, cnst.Column);
            }
        }

        [SetTag("expression")]
        public static Expression ReadNewOperator(
            [Keywords("new")] Parsed<string> kwNew,
            [RequireTags("type")] TypeExpression type,
            [Keywords("[")] string brOpen,
            [RequireTags("expression")] Expression count,
            [Keywords("]")] string brClose
            )
        {
            return new NewExpression(kwNew.Line, kwNew.Column, type, count);
        }

        [SetTag("expression")]
        public static Expression ReadCastExpession(
            [Keywords("<")] Parsed<string> typeOpen,
            [RequireTags("type")] TypeExpression typeExpression,
            [Keywords(">")] string typeClose,
            [Keywords("(")] string exprOpen,
            [RequireTags("expression")] Expression expression,
            [Keywords(")")] string exprClose
            )
        {
            return new CastExpression(typeOpen.Line, typeOpen.Column, typeExpression, expression);
        }

        [SetTag("statement")]
        public static Statement ReadDeleteStatement(
            [Keywords("delete")] Parsed<string> kwDelete,
            [RequireTags("id")] string id,
            [Keywords(";")] string end
            )
        {
            return new DeleteStatement(kwDelete.Line, kwDelete.Column, id);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class ExpressionStatement : Statement
    {
        public TypeExpression? InitType { get; }
        public string? VariableName { get; }
        public Expression Expression { get; }

        public ExpressionStatement(
            int line,
            int column,
            TypeExpression? initType,
            string? variableName,
            Expression expression
            )
            : base(line, column)
        {
            InitType = initType;
            VariableName = variableName;
            Expression = expression;
        }

        public override void Compile(CompilationManager cm)
        {
            var rt = Expression.Type(cm);
            int? handle = null;
            string output;
            if(InitType is not null && VariableName is not null)
            {
                if (!rt.IsAssignableTo(InitType))
                    throw new CompilationError("Incompatible types", Line, Column);
                var found = cm.SemanticNetwork.Search(cm.Scope, "@get-current", VariableName);
                if (found.Count != 0)
                    throw new CompilationError("Variable not found", Line, Column);
                output = cm.AllocUniqueVariable(InitType.VMType, VariableName);
            }
            else
            {
                output = cm.GetTempVariable(rt.VMType, out int htmp);
                handle = htmp;
            }

            Expression.CompileRight(cm, output);
            if(InitType is not null && VariableName is not null)
                _ = new LocalVariable(InitType, VariableName, cm.Scope, output, Line, Column);

            if (handle is int h)
                cm.FreeTempVariable(h);
        }
    }
}

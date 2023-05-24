using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicAnalyzer
{
    internal class VMInfo
    {
        private readonly SortedDictionary<(string, int), VariableDeclaration[]> parameters = new();
        private readonly SortedDictionary<(string, int), VariableDeclaration[]> variables = new();

        public void AddFunction(
            string name, 
            int paramCount, 
            IEnumerable<VariableDeclaration> parameters, 
            IEnumerable<VariableDeclaration> variables)
        {
            var signature = (name, paramCount);
            this.parameters.Add(signature, parameters.ToArray());
            this.variables.Add(signature, parameters.Concat(variables).ToArray());
        }

        public Frame MakeFrame(string fname, int count, params long[] args)
        {
            Frame f = new(variables[(fname, count)]);
            var parameters = this.parameters[(fname, count)];
            for (int i = 0; i < count; i++)
                f.SetValue(parameters[i].Name, args[i]);
            return f;
        }
    }
}

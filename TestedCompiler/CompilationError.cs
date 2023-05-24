using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class CompilationError : Exception
    {
        public CompilationError(string message, int line, int column) : base($"[{line}:{column}]{message}") { }
    }
}

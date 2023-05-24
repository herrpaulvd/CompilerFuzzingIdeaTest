using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestedCompiler
{
    internal abstract class Statement : CodeObject
    {
        protected Statement(int line, int column) : base(line, column)
        {
        }

        public abstract void Compile(CompilationManager cm);
    }
}

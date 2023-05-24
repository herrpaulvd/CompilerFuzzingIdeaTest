using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    // temporary only void main()
    internal class Function : CodeObject
    {
        public Statement Block { get; }

        public Function(int line, int column, Statement block)
            : base(line, column)
        {
            Block = block;
        }

        public CompilationManager Compile(bool bugOF, bool bugMem)
        {
            CompilationManager cm = new(bugOF, bugMem);
            Block.Compile(cm);
            cm.WriteCode($"ret");
            return cm;
        }
    }
}

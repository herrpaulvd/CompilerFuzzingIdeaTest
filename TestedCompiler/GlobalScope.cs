using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class GlobalScope : CodeObject
    {
        public Function Main { get; }

        public GlobalScope(Function main)
            : base(main.Line, main.Column)
        {
            Main = main;
        }

        public string Compile(bool bugOF, bool bugMem)
        {
            return Main.Compile(bugOF, bugMem).Release();
        }
    }
}

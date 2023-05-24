using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal abstract class CodeObject
    {
        public int Line { get; private set; }
        public int Column { get; private set; }

        protected CodeObject(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public CodeObject ReplaceXY(int line, int column)
        {
            Line = line;
            Column = column;
            return this;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class ExeWriter
    {
        private string fmt;
        private List<StringBuilder> sections = new();

        public ExeWriter(string structure)
        {
            fmt = structure;
        }

        public void WriteLine(int section, string text, int tabs)
        {
            while (sections.Count <= section)
                sections.Add(new());
            var output = (sections[section] ??= new());
            for (int i = 0; i < tabs; i++) output.Append('\t');
            output.Append(text);
            output.Append('\n');
        }

        public string Release() => string.Format(fmt, sections.Select(s => (object)s).ToArray());
        
    }
}

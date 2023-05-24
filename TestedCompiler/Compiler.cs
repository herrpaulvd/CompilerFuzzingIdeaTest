using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CompileLib.Parsing;

namespace TestedCompiler
{
    public static class Compiler
    {
        private static ParsingEngine engine = new ParsingEngineBuilder()
            .AddToken("id", @"[[:alpha:]_][[:alnum:]_]*")
            .AddToken("int", @"[0-9]+")
            .AddToken(SpecialTags.TAG_SKIP, "[[:space:]]")
            .AddToken(SpecialTags.TAG_SKIP, @"//[^[:cntrl:]]*")
            .AddProductions<Syntax>()
            .Create("program");

        public static string? Compile(IEnumerable<char> input, bool bugOF, bool bugMem)
        {
            try
            {
                var global = engine.Parse<GlobalScope>(input).Self;
                return global?.Compile(bugOF, bugMem);
            }
            catch (Exception ex)
            {
                Console.WriteLine("There is an error:");
                Console.WriteLine(ex.GetType());
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}

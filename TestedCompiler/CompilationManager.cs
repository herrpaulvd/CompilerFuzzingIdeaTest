using CompileLib.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TestedCompiler
{
    internal class CompilationManager
    {
        public readonly SemanticNetwork SemanticNetwork = new(Resources.SearchScript);
        
        private readonly Stack<LocalScope> scopes = new();
        public LocalScope Scope => scopes.Peek();
        public void PushScope() => scopes.Push(new(Scope));
        public void PopScope() => scopes.Pop();

        private readonly ExeWriter Output = new ExeWriter("{0}fun main\nparam\nlocal\n{1}{3}do{2}{4}end\n");

        private const int SECTION_EXTRA_FUN = 0;
        private const int SECTION_FRAME_VAR = 1;
        private const int SECTION_FRAME_INIT = 2;
        private const int SECTION_VAR = 3;
        private const int SECTION_CODE = 4;
        private const int NESTED_TAB = 1;
        private const string framevar = "framevar";

        // labels manager
        private int labelCount = 0;

        public string DefineLabel() => $"L{labelCount++}";

        // output var manager
        private long commonFrameSize = 8;
        private int uniqueCount = 0;
        public string AllocUniqueVariable(string type, string originalName)
        {
            string result = $"v_{uniqueCount++}_{type}_{originalName}";
            Output.WriteLine(SECTION_VAR, $"{type} {result}", NESTED_TAB);
            commonFrameSize += Options.Size[type];
            return result;
        }

        private List<string> temps = new();
        private List<string> temptypes = new();
        private SortedDictionary<string, Stack<int>> free = new();

        public string GetTempVariable(string type, out int handle)
        {
            var allowed = free[type];
            if (allowed.TryPop(out handle))
            {
                return temps[handle];
            }
            else
            {
                handle = temps.Count;
                var result = $"v_tmp_{handle}_{type}";
                commonFrameSize += Options.Size[type];
                temps.Add(result);
                temptypes.Add(type);
                Output.WriteLine(SECTION_VAR, $"{type} {result}", NESTED_TAB);
                return result;
            }
        }

        public void FreeTempVariable(int handle)
        {
            free[temptypes[handle]].Push(handle);
        }

        public void WriteCode(string code) => Output.WriteLine(SECTION_CODE, code, NESTED_TAB);

        public readonly bool BugOF;
        public readonly bool BugMem;

        public CompilationManager(bool bugOF, bool bugMem)
        {
            scopes.Push(new(null));
            foreach (var t in Options.VMTypes)
                free.Add(t, new());
            Output.WriteLine(SECTION_EXTRA_FUN, Resources.ExtraFunctions, 0);
            Output.WriteLine(SECTION_FRAME_VAR, $"long {framevar}", NESTED_TAB);
            BugOF = bugOF;
            BugMem = bugMem;
        }

        public bool SafeMode => true;

        public void SafeNew(string vcount, string output)
        {
            if(SafeMode)
            {
                string vaddr = GetTempVariable("ptr", out int haddr);
                WriteCode($"addr {output} {vaddr}");
                WriteCode($"call SafeNew 2 {vcount} {vaddr}");
            }
            else
            {
                WriteCode($"new {vcount} {output}");
            }
        }

        public void SafeDelete(string todel)
        {
            if(SafeMode)
            {
                WriteCode($"call SafeDelete 1 {todel}");
            }
            else
            {
                WriteCode($"del {todel}");
            }
        }

        public void SafeRead(string src, string dst)
        {
            if(SafeMode)
                WriteCode($"call SafeAddr 1 {src}");
            WriteCode($"read {src} {dst}");
        }

        public void SafeWrite(string src, string dst)
        {
            if (SafeMode)
                WriteCode($"call SafeAddr 1 {dst}");
            WriteCode($"write {src} {dst}");
        }

        private static Dictionary<string, string>
            OverflowCheckerNames = new()
            {
                { "+", "CheckAddOverflow" },
                { "-", "CheckSubOverflow" },
                { "*", "CheckMulOverflow" },
                { "/", "CheckDivOverflow" },
                { "%", "CheckDivOverflow" },
            };

        private static (long, long) GetMinMax(int size)
        {
            long maxres = (1L << (size * 8 - 1)) - 1;
            long minres = -maxres - 1;
            return (minres, maxres);
        }

        public void AddOverflowChecker(
            string left,
            string right,
            TypeExpression resultType,
            string operation
            )
        {
            if(SafeMode)
            {
                if (OverflowCheckerNames.TryGetValue(operation, out string? fname))
                {
                    var (min, max) = GetMinMax(resultType.Size);
                    WriteCode($"call {fname} 4 {left} {right} {min} {max}");
                }
            }
        }

        public string Release()
        {
            Output.WriteLine(SECTION_FRAME_INIT, $"mov {commonFrameSize} {framevar}", NESTED_TAB);
            return Output.Release();
        }
    }
}

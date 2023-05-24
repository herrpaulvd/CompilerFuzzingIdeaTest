using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicAnalyzer
{
    public static class Analyzer
    {
        private static (long, long) GetMinMax(int size)
        {
            long maxres = (1L << (size * 8 - 1)) - 1;
            long minres = -maxres - 1;
            return (minres, maxres);
        }

        private static bool OverflowAdd(long left, long right, int resultSize)
        {
            var (min, max) = GetMinMax(resultSize);
            return
                (right > 0 && left > max - right)
                || (right < 0 && left < min - right);
        }

        private static bool OverflowSub(long left, long right, int resultSize)
        {
            var (min, max) = GetMinMax(resultSize);
            return
                (right < 0 && left > max + right)
                || (right > 0 && left < min + right);
        }

        private static bool OverflowMul(long left, long right, int resultSize)
        {
            var (min, max) = GetMinMax(resultSize);
            return
                (left == -1 && right == min)
                || (right == -1 && left == min)
                || (right != 0 && left > max / right)
                || (right != 0 && left < min / right);
        }

        private static bool OverflowDiv(long left, long right, int resultSize)
        {
            var (min, _) = GetMinMax(resultSize);
            return left == min && right == -1;
        }

        private static VMInfo ReadCompilation(IEnumerator<string[]> reader)
        {
            VMInfo result = new();
            string? fname = null;
            int fcount = 0;
            List<VariableDeclaration> parameters = new();
            List<VariableDeclaration> locals = new();

            void TryReleaseFunction()
            {
                if (fname is not null)
                {
                    result.AddFunction(fname, fcount, parameters, locals);
                    parameters.Clear();
                    locals.Clear();
                }
            }

            while (reader.MoveNext())
            {
                var line = reader.Current;
                switch(line[0])
                {
                    case "execution":
                        TryReleaseFunction();
                        return result;
                    case "compile":
                        TryReleaseFunction();
                        fname = line[1];
                        fcount = int.Parse(line[2]);
                        break;
                    case "foundparam":
                        parameters.Add(new(line[1], line[2]));
                        break;
                    case "foundlocal":
                        locals.Add(new(line[1], line[2]));
                        break;
                    case "error":
                        throw new ErrorException("VM COMPILATION ERROR: " + line[1]);
                }
            }

            return result;
        }

        private static long StringToLong(string s)
        {
            if(s.StartsWith("mem"))
            {
                var splitted = s.Split(':');
                return (long.Parse(splitted[1]) << 32) + long.Parse(splitted[2]);
            }
            return long.Parse(s);
        }

        private static readonly SortedSet<string> unusualCases = new()
        {
            "ret", "end", "label", "goto", "throw", // no ability to be corrupted
            "call", // because call is a separate case
            "gotoif", // too
            "add", "sub", "mul", "div", "mod" // separate cases too
        };

        private static void ReadExecution(VMInfo vminfo, Frame frame, IEnumerator<string[]> reader)
        {
            // 3 execution
            // check:
            // exe (add|sub|mul|div|mod) => check overflow, update corrupted for some subset of ins
            // exe call - rec fun
            // set - not overprove, trust it
            // attention: params & non-temp locals (find by name starts not with "v_tmp")
            // but convert mem:x:y -> long
            // error - return false immediately

            long OperandValue(string s)
                => frame.Exists(s) ? frame.GetValue(s) : StringToLong(s);

            ErrorException corruptionReport(string variable)
            {
                return new($"Critical variable {variable} becomes corrupted because of overflow");
            }

            // true if result is critical & corrupted
            void CheckInheritCorruption(string[] instruction)
            {
                if (frame.InheritCorruption(
                    out bool critical,
                    instruction[^1],
                    instruction.Skip(1).Take(instruction.Length - 2).Where(v => frame.Exists(v))))
                    throw corruptionReport(instruction[^1]);
            }

            while(reader.MoveNext())
            {
                var line = reader.Current;
                switch(line[0])
                {
                    case "exe":
                        var instruction = line[1].Split('.');
                        switch(instruction[0])
                        {
                            case "call":
                                var corruptedVar = instruction.Skip(3).FirstOrDefault(v => frame.Exists(v) && frame.IsCorrupted(out _, v));
                                if (corruptedVar is not null)
                                    throw new ErrorException($"Calling function {instruction[1]} {instruction[2]} with corrupted parameter {corruptedVar}");
                                int count = int.Parse(instruction[2]);
                                long[] args = new long[count];
                                for(int i = 0; i < count; i++)
                                    args[i] = OperandValue(instruction[i + 3]);
                                var childFrame = vminfo.MakeFrame(instruction[1], int.Parse(instruction[2]), args);
                                ReadExecution(vminfo, childFrame, reader);
                                break;
                            case "add":
                                CheckInheritCorruption(instruction);
                                var left = OperandValue(instruction[1]);
                                var right = OperandValue(instruction[2]);
                                var result = instruction[3];
                                var size = frame.GetSize(result);
                                if(OverflowAdd(left, right, size))
                                {
                                    frame.MarkCorrupted(out bool critical, result);
                                    if (critical) throw corruptionReport(result);
                                }
                                break;
                            case "sub":
                                CheckInheritCorruption(instruction);
                                left = OperandValue(instruction[1]);
                                right = OperandValue(instruction[2]);
                                result = instruction[3];
                                size = frame.GetSize(result);
                                if (OverflowSub(left, right, size))
                                {
                                    frame.MarkCorrupted(out bool critical, result);
                                    if (critical) throw corruptionReport(result);
                                }
                                break;
                            case "mul":
                                CheckInheritCorruption(instruction);
                                left = OperandValue(instruction[1]);
                                right = OperandValue(instruction[2]);
                                result = instruction[3];
                                size = frame.GetSize(result);
                                if (OverflowMul(left, right, size))
                                {
                                    frame.MarkCorrupted(out bool critical, result);
                                    if (critical) throw corruptionReport(result);
                                }
                                break;
                            case "div":
                            case "mod":
                                CheckInheritCorruption(instruction);
                                left = OperandValue(instruction[1]);
                                right = OperandValue(instruction[2]);
                                result = instruction[3];
                                size = frame.GetSize(result);
                                if (OverflowDiv(left, right, size))
                                {
                                    frame.MarkCorrupted(out bool critical, result);
                                    if (critical) throw corruptionReport(result);
                                }
                                break;
                            case "gotoif":
                                var cond = instruction[1];
                                if (frame.Exists(cond)
                                    && frame.IsCorrupted(out _, cond))
                                    throw corruptionReport(cond);
                                break;
                        }

                        if (!unusualCases.Contains(instruction[0]))
                            CheckInheritCorruption(instruction);
                        break;
                    case "set":
                        frame.SetValue(line[1], StringToLong(line[2]));
                        break;
                    case "error":
                        throw new ErrorException("VM EXECUTION ERROR: " + line[1]);
                    case "exit":
                        return;
                }
            }
        }

        public static bool Check(string vmoutput, out string? error)
        {
            var lines = Array.ConvertAll(vmoutput.Split('\n'), l => l.Split(' '));
            IEnumerator<string[]> reader = (lines as IEnumerable<string[]>).GetEnumerator();
            try
            {
                var vminfo = ReadCompilation(reader);
                var frame = vminfo.MakeFrame("main", 0);
                error = null;
                ReadExecution(vminfo, frame, reader);
                return true;
            }
            catch(ErrorException e)
            {
                error = e.Message;
                return false;
            }
        }
    }
}

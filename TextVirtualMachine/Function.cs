using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal class Function
    {
        private const string MemError = "mem_access_denied";
        public readonly string Name;
        public readonly ReadOnlyCollection<Type> ParameterTypes;
        private readonly IList<Type> AllTypes;
        private readonly IList<string> AllNames;
        private readonly IList<Action<Frame>> Instructions;
        private readonly FrameBuilder frameBuilder;

        private static Type ParseType(string name)
            => name switch
            {
                "ptr" => typeof(Pointer),
                "byte" => typeof(sbyte),
                "short" => typeof(short),
                "int" => typeof(int),
                "long" => typeof(long),
                _ => throw new Interruption("invalid_type_name")
            };

        private static void LogExe(string[] ins)
            => Log.AddRecord("exe", string.Join('.', ins));

        private static Func<Frame, long> GetArgumentValue(
            string opArgument, 
            IDictionary<string, int> allNamesSet)
        {
            if (allNamesSet.ContainsKey(opArgument))
                return f => f.GetVariable(opArgument).LongValue;
            else if (long.TryParse(opArgument, out long longval))
                return _ => longval;
            else
                throw new Interruption("invalid_operand");
        }

        private static Action<Frame> PerformUnaryOperation(
            string[] ins,
            Func<long, long> operation,
            IDictionary<string, int> allNamesSet
            )
        {
            string opResult = ins[2];
            if (allNamesSet.ContainsKey(opResult))
            {
                Func<Frame, long> getArgument = GetArgumentValue(ins[1], allNamesSet);

                return f =>
                {
                    LogExe(ins);
                    var dstvar = f.GetVariable(opResult);
                    dstvar.LongValue = operation(getArgument(f));
                    Log.AddRecord("set", opResult, dstvar.ShowValue());
                    f.IP++;
                };
            }
            else
                throw new Interruption("invalid_destination");
        }

        private static Action<Frame> PerformBinaryOperation(
            string[] ins,
            Func<long, long, long> operation,
            IDictionary<string, int> allNamesSet
            )
        {
            string opResult = ins[3];
            if (allNamesSet.ContainsKey(opResult))
            {
                Func<Frame, long> getArg1 = GetArgumentValue(ins[1], allNamesSet);
                Func<Frame, long> getArg2 = GetArgumentValue(ins[2], allNamesSet);

                return f =>
                {
                    LogExe(ins);
                    var dstvar = f.GetVariable(opResult);
                    try
                    {
                        dstvar.LongValue = operation(getArg1(f), getArg2(f));
                    }
                    catch(DivideByZeroException)
                    {
                        throw new Interruption("divide_by_zero");
                    }
                    Log.AddRecord("set", opResult, dstvar.ShowValue());
                    f.IP++;
                };
            }
            else
                throw new Interruption("invalid_destination");
        }

        private Action<Frame> ParseInstruction(
            string[] ins, 
            IDictionary<string, int> labels, 
            IDictionary<string, int> allNamesSet)
        {
            bool isPtr(string vname) => AllTypes[allNamesSet[vname]] == typeof(Pointer);
            Log.AddRecord("instruction", string.Join('.', ins));
            switch (ins[0])
            {
                case "ret":
                    return _ =>
                    {
                        LogExe(ins);
                        throw new RetException(true);
                    };
                case "end":
                    return _ =>
                    {
                        LogExe(ins);
                        throw new RetException(false);
                    };
                case "label":
                    string l = ins[1];
                    return f =>
                    {
                        Log.AddRecord("reached", l);
                        f.IP++;
                    };
                case "goto":
                    if (labels.TryGetValue(ins[1], out int nextIP))
                        return f =>
                        {
                            LogExe(ins);
                            f.IP = nextIP;
                        };
                    else
                        throw new Interruption("invalid_goto_label");
                case "del":
                    string delArg = ins[1];
                    if (allNamesSet.ContainsKey(delArg))
                    {
                        if (isPtr(delArg))
                            return f =>
                            {
                                LogExe(ins);
                                var v = f.GetVariable(delArg);
                                var ptrval = v.PtrValue;
                                var seg = Segment.Get(ptrval.segIndex);
                                if (seg is Frame)
                                    throw new Interruption("del_frame");
                                if (ptrval.segOffset != 0)
                                    throw new Interruption("del_not_seg_start");
                                Log.AddRecord("del", ptrval.Name);
                                seg?.Free();
                                f.IP++;
                            };
                        else
                            throw new Interruption("invalid_operand");
                    }
                    else
                        throw new Interruption("invalid_operand");
                case "throw":
                    return f =>
                    {
                        LogExe(ins);
                        throw new VMException(ins[1]);
                    };
                case "mov":
                    return PerformUnaryOperation(ins, x => x, allNamesSet);
                case "lnot":
                    return PerformUnaryOperation(ins, x => ~x, allNamesSet);
                case "not":
                    return PerformUnaryOperation(ins, x => x == 0 ? 1 : 0, allNamesSet);
                case "read":
                    string readOpSrc = ins[1];
                    string readOpDst = ins[2];
                    if (!allNamesSet.ContainsKey(readOpSrc) 
                        || !isPtr(readOpSrc))
                        throw new Interruption("invalid_source");
                    if (!allNamesSet.ContainsKey(readOpDst))
                        throw new Interruption("invalid_destination");
                    return f =>
                    {
                        LogExe(ins);
                        var srcvar = f.GetVariable(readOpSrc);
                        var dstvar = f.GetVariable(readOpDst);
                        Log.AddRecord("readfrom ", srcvar.PtrValue.Name);
                        var srcptr = srcvar.PtrValue.GetAccess(dstvar.Size) ?? throw new Interruption(MemError);
                        var dstptr = dstvar.GetAccess();
                        for (int i = 0; i < dstvar.Size; i++)
                            dstptr[i] = srcptr[i];
                        f.IP++;
                    };
                case "write":
                    string writeOpSrc = ins[1];
                    string writeOpDst = ins[2];
                    if (!allNamesSet.ContainsKey(writeOpSrc))
                        throw new Interruption("invalid_source");
                    if (!allNamesSet.ContainsKey(writeOpDst)
                        || !isPtr(writeOpDst))
                        throw new Interruption("invalid_destination");
                    return f =>
                    {
                        LogExe(ins);
                        var srcvar = f.GetVariable(writeOpSrc);
                        var dstvar = f.GetVariable(writeOpDst);
                        Log.AddRecord("writeto ", dstvar.PtrValue.Name);
                        var srcptr = srcvar.GetAccess();
                        var dstptr = dstvar.PtrValue.GetAccess(srcvar.Size) ?? throw new Interruption(MemError);
                        for (int i = 0; i < srcvar.Size; i++)
                            dstptr[i] = srcptr[i];
                        f.IP++;
                    };
                case "new":
                    var newDst = ins[2];
                    if (!allNamesSet.ContainsKey(newDst))
                        throw new Interruption("invalid_destination");
                    return PerformUnaryOperation(ins, size =>
                    {
                        Segment result = new((int)size);
                        var ptr = result.GetPointer(0);
                        Log.AddRecord("alloc", ptr.Name);
                        return ptr.longValue;
                    }, allNamesSet);
                case "addr":
                    var addrDst = ins[2];
                    if (!allNamesSet.ContainsKey(addrDst))
                        throw new Interruption("invalid_destination");
                    var addrName = ins[1];
                    if (!allNamesSet.ContainsKey(addrName))
                        throw new Interruption("invalid_variable_name");
                    return f =>
                    {
                        LogExe(ins);
                        var v = f.GetVariable(addrName);
                        var dst = f.GetVariable(addrDst);
                        dst.PtrValue = v.Location;
                        Log.AddRecord("set", addrDst, dst.ShowValue());
                        f.IP++;
                    };
                case "gotoif":
                    var gotoIfFlag = ins[1];
                    if (!allNamesSet.ContainsKey(gotoIfFlag))
                        throw new Interruption("invalid_variable_name");
                    var gotoIfLabel = ins[2];
                    if(labels.TryGetValue(gotoIfLabel, out int gotoIfNext))
                    {
                        return f =>
                        {
                            LogExe(ins);
                            var flag = f.GetVariable(gotoIfFlag);
                            if (flag.LongValue == 0)
                                f.IP++;
                            else
                                f.IP = gotoIfNext;
                        };
                    }
                    else
                        throw new Interruption("invalid_label");
                case "badseg":
                    var argvalf = GetArgumentValue(ins[1], allNamesSet);
                    var output = ins[2];
                    if (allNamesSet.ContainsKey(output))
                    {
                        return f =>
                        {
                            LogExe(ins);
                            var v = f.GetVariable(output);
                            v.LongValue = (Segment.Get((int)argvalf(f))?.Alive ?? false) ? 0 : 1;
                            Log.AddRecord("set", v.Name, v.ShowValue());
                            f.IP++;
                        };
                    }
                    else
                        throw new Interruption("invalid_operand");
                case "add":
                    return PerformBinaryOperation(ins, (x, y) => x + y, allNamesSet);
                case "sub":
                    return PerformBinaryOperation(ins, (x, y) => x - y, allNamesSet);
                case "mul":
                    return PerformBinaryOperation(ins, (x, y) => x * y, allNamesSet);
                case "div":
                    return PerformBinaryOperation(ins, (x, y) => x / y, allNamesSet);
                case "mod":
                    return PerformBinaryOperation(ins, (x, y) => x % y, allNamesSet);
                case "less":
                    return PerformBinaryOperation(ins, (x, y) => x < y ? 1 : 0, allNamesSet);
                case "greater":
                    return PerformBinaryOperation(ins, (x, y) => x > y ? 1 : 0, allNamesSet);
                case "lesseq":
                    return PerformBinaryOperation(ins, (x, y) => x <= y ? 1 : 0, allNamesSet);
                case "greatereq":
                    return PerformBinaryOperation(ins, (x, y) => x >= y ? 1 : 0, allNamesSet);
                case "eq":
                    return PerformBinaryOperation(ins, (x, y) => x == y ? 1 : 0, allNamesSet);
                case "neq":
                    return PerformBinaryOperation(ins, (x, y) => x != y ? 1 : 0, allNamesSet);
                case "and":
                    return PerformBinaryOperation(ins, (x, y) => (x != 0 && y != 0) ? 1 : 0, allNamesSet);
                case "or":
                    return PerformBinaryOperation(ins, (x, y) => (x != 0 || y != 0) ? 1 : 0, allNamesSet);
                case "xor":
                    return PerformBinaryOperation(ins, (x, y) => ((x != 0) != (y != 0)) ? 1 : 0, allNamesSet);
                case "land":
                    return PerformBinaryOperation(ins, (x, y) => x & y, allNamesSet);
                case "lor":
                    return PerformBinaryOperation(ins, (x, y) => x | y, allNamesSet);
                case "lxor":
                    return PerformBinaryOperation(ins, (x, y) => x ^ y, allNamesSet);
                case "shl":
                    return PerformBinaryOperation(ins, (x, y) => (x << (int)y), allNamesSet);
                case "shr":
                    return PerformBinaryOperation(ins, (x, y) => (x >> (int)y), allNamesSet);
                case "call":
                    string fname = ins[1];
                    int argcnt = ins.Length - 3;
                    var preArgs = new Func<Frame, long>[argcnt];
                    for (int i = 0; i < argcnt; i++)
                        preArgs[i] = GetArgumentValue(ins[i + 3], allNamesSet);
                    return frame =>
                    {
                        LogExe(ins);
                        if (VMEnvironment.Functions.TryGetValue((fname, argcnt), out Function? func))
                        {
                            if (func is null) throw new Interruption("internal_error_null_func");
                            func.Call(Array.ConvertAll(preArgs, gv => gv(frame)));
                            frame.IP++;
                        }
                        else
                            throw new Interruption("function_is_not_found");
                    };
                default:
                    throw new Interruption("internal_error_invalid_instruction");
            }
        }

        public Function(string name, IList<VariableDeclaration> parameters, IList<VariableDeclaration> locals, IList<string[]> instructions)
        {
            Name = name;
            
            var allArray = parameters.Concat(locals).ToArray();
            AllNames = Array.ConvertAll(allArray, v => v.Name);
            var allNamesSet = new SortedDictionary<string, int>();
            for(int i = 0; i < AllNames.Count; i++)
                if (!allNamesSet.TryAdd(AllNames[i], i))
                    throw new Interruption("duplicate_names");

            AllTypes = new ReadOnlyCollection<Type>(allArray.Select(v => ParseType(v.Type)).ToArray());
            ParameterTypes = new ReadOnlyCollection<Type>(AllTypes.Take(parameters.Count).ToArray());

            Log.AddRecord("compile", Name, ParameterTypes.Count.ToString());
            foreach (var vd in parameters)
                Log.AddRecord("foundparam", vd.Type, vd.Name);
            foreach (var vd in locals)
                Log.AddRecord("foundlocal", vd.Type, vd.Name);

            Instructions = new Action<Frame>[instructions.Count];
            SortedDictionary<string, int> labels = new();
            for(int i = 0; i < instructions.Count; i++)
            {
                var ins = instructions[i];
                if (ins[0] == "label")
                {
                    if (labels.ContainsKey(ins[1]))
                        throw new Interruption("duplicate_labels");
                    else
                        labels.Add(ins[1], i);
                }
            }

            
            for (int i = 0; i < instructions.Count; i++)
                Instructions[i] = ParseInstruction(instructions[i], labels, allNamesSet);
            
            int[] allSizes = AllTypes.Select(t => Marshal.SizeOf(t)).ToArray();
            bool[] isPtr = AllTypes.Select(t => t == typeof(Pointer)).ToArray();
            frameBuilder = new(AllNames, allSizes, isPtr);
        }

        public void Call(params long[] args)
        {
            Frame f = frameBuilder.MakeFrame();
            for(int i = 0; i < args.Length;  i++)
                f.GetVariable(AllNames[i]).LongValue = args[i];

            try
            {
                while (true)
                    Instructions[f.IP](f);
            }
            catch(RetException e)
            {
                if(e.Correct)
                    Log.AddRecord("exit", Name);
                else
                    throw new Interruption("function_finished_without_ret");
            }
            f.Free();
        }
    }
}

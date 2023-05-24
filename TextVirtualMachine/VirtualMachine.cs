using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    public static class VirtualMachine
    {
        public static string Execute(string input, int recordsLimit = -1)
        {
            Log.Initialize(recordsLimit);
            var codestr = input.Split(new char[] { ' ', '\n', '\t', '\r' }).Where(s => s.Length > 0);

            string? MaybeRead(IEnumerator<string> codee)
            {
                return codee.MoveNext() ? codee.Current : null;
            }

            string StrictRead(IEnumerator<string> codee)
            {
                return codee.MoveNext() ? codee.Current : throw new Interruption("unexpected_eof");
            }

            string[] readInstruction(IEnumerator<string> codee)
            {
                string type = StrictRead(codee);
                if (type == "call")
                {
                    string name = StrictRead(codee);
                    string scnt = StrictRead(codee);
                    if (!int.TryParse(scnt, out int cnt))
                        throw new Interruption("expected_int_param_count");
                    if (cnt < 0)
                        throw new Interruption("negative_int_param_count");
                    string[] res = new string[cnt + 3];
                    res[0] = type;
                    res[1] = name;
                    res[2] = scnt;
                    for (int i = 0; i < cnt; i++)
                        res[i + 3] = StrictRead(codee);
                    return res;
                }

                int n = type switch
                {
                    "ret" or "end" => 0,
                    "label" or "goto" or "del" or "throw" => 1,
                    "mov" or "not" or "lnot" or "read" or "write" or "new" or "addr" or "gotoif"
                    or "badseg" => 2,
                    "add" or "sub" or "mul" or "div" or "mod"
                    or "less" or "greater" or "lesseq" or "greatereq" or "eq" or "neq"
                    or "and" or "or" or "xor"
                    or "land" or "lor" or "lxor"
                    or "shl" or "shr" => 3,
                    _ => throw new Interruption("unexpected_instruction")
                } + 1;
                string[] result = new string[n];
                result[0] = type;
                for (int i = 1; i < n; i++)
                    result[i] = StrictRead(codee);
                return result;
            }

            string[] allowedTypes =
            {
                "byte",
                "short",
                "int",
                "long",
                "ptr"
            };

            List<VariableDeclaration> readVariables(IEnumerator<string> codee, string endWord)
            {
                List<VariableDeclaration> result = new();
                while (true)
                {
                    var type = StrictRead(codee);
                    if (type == endWord) return result;
                    if (!allowedTypes.Contains(type))
                        throw new Interruption("invalid_type");
                    var name = StrictRead(codee);
                    result.Add(new(name, type));
                }
            }

            Function? readFunction(IEnumerator<string> codee)
            {
                var kw = MaybeRead(codee);
                if (kw is null) return null;
                if (kw != "fun") throw new Interruption("keyword_fun_required");
                var name = StrictRead(codee);
                var paramkw = StrictRead(codee);
                if (paramkw != "param") throw new Interruption("keyword_param_required");
                var parameters = readVariables(codee, "local");
                var locals = readVariables(codee, "do");
                List<string[]> instructions = new();
                while (true)
                {
                    var i = readInstruction(codee);
                    instructions.Add(i);
                    if (i[0] == "end")
                        return new Function(name, parameters, locals, instructions);
                }
            }

            // maincode
            try
            {
                VMEnvironment.Initialize();
                Log.AddRecord("compilation");
                var en = codestr.GetEnumerator();
                while (true)
                {
                    Function? f = readFunction(en);
                    if (f is null)
                        break;
                    else
                        VMEnvironment.Functions.Add((f.Name, f.ParameterTypes.Count), f);
                }
                if (VMEnvironment.Functions.TryGetValue(("main", 0), out var fu))
                {
                    Log.AddRecord("execution");
                    fu.Call();
                }
                else
                    throw new Interruption("no_main_0");
            }
            catch (Interruption)
            {
                //Console.WriteLine("An interruption occured");
            }
            catch(VMException)
            {
                //Console.WriteLine("An exception throwed");
            }
            catch(LimitReachedException)
            {
                //Console.WriteLine("Reached limit of records");
            }

            return Log.Release();
        }
    }
}

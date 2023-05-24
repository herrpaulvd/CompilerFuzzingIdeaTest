using TestedCompiler;
using TextVirtualMachine;
using BytecodeDecoder;
using DynamicAnalyzer;

List<PredefinedVariable> predef = null;

bool terminateProcess = false;

void TimerCallback(object? _) => terminateProcess = true;

const int minutes = 60;
const int min2ms = 60 * 1000;

Timer timer = new(TimerCallback, null, minutes * min2ms, 0);

var begin = DateTime.Now;

Exception GetException(string message)
{
    var end = DateTime.Now;
    Console.WriteLine((end - begin).TotalSeconds);
    return new(message);
}

WinSharpFuzz.Fuzzer.LibFuzzer.Initialize(() =>
{
    predef = new()
    {
        {new("a0", 0, "0") },
        {new("b0", 0, "1") },
        {new("c0", 0, "10") },
        {new("a1", 1, "<long*>(0)") },
        {new("b1", 1, "new long[10]") },
        {new("c1", 1, "new long[100]") },
        {new("a2", 2, "new long*[1]") },
        {new("b2", 2, "new long*[10]") },
        {new("c2", 2, "new long*[100]") },
        {new("a3", 3, "new long**[10]") },
        {new("a4", 4, "new long***[10]") },
    };
});

WinSharpFuzz.Fuzzer.LibFuzzer.Run(
    input =>
    {
        if (terminateProcess)
            throw GetException("Timeout");

        string compilerInput = Decoder.Decode(input.ToArray(), predef);

        string compilerOutput = Compiler.Compile(compilerInput, false, true) ?? throw new Exception("Invalid compiler input");
        string vmoutput = VirtualMachine.Execute(compilerOutput, 2048 * 16);
        Analyzer.Check(vmoutput, out string? error);

        if (error is not null)
            throw GetException("Analyzer error: " + error);
    });



using TestedCompiler;
using TextVirtualMachine;
using BytecodeDecoder;
using DynamicAnalyzer;

List<PredefinedVariable> predef = new()
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

bool terminateProcess = false;

void TimerCallback(object _) => terminateProcess = true;

const int minutes = 60;
const int min2ms = 60 * 1000;

//Timer timer = new(TimerCallback, null, minutes * min2ms, 0);

Random random = new(0);
for(int test = 1; ; test++)
{
    if (test % 10 == 0) Console.WriteLine("TEST " + test);
    try
    {
        int count = random.Next(257);
        byte[] input = new byte[count];
        random.NextBytes(input);

        if (terminateProcess)
            throw new Exception("Timeout");

        string compilerInput = Decoder.Decode(input.ToArray(), predef);

        string compilerOutput = Compiler.Compile(compilerInput, true) ?? throw new Exception("Invalid compiler input");
        string vmoutput = VirtualMachine.Execute(compilerOutput, 2048);
        Analyzer.Check(vmoutput, out string? error);

        if (error is not null)
            throw new Exception("Analyzer error: " + error);
    }
    catch(Exception)
    {
        Console.WriteLine("FOUND");
    }
}

using TestedCompiler;
using TextVirtualMachine;
using BytecodeDecoder;
using DynamicAnalyzer;

Random random = new(0);
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

const int SKIPTILL = 0;

var begin = System.DateTime.Now;
for(int test = 1; /*test <= 100*/; test++)
{
    //int count = random.Next(1024);
    //byte[] input = new byte[count];
    //random.NextBytes(input);
    var input = File.ReadAllBytes("c7");
    if (test < SKIPTILL) continue;
    string compilerInput = Decoder.Decode(input, predef);
    
    Console.Clear();
    //Console.WriteLine(compilerInput);
    //Console.ReadKey();
    
    string? compilerOutput = Compiler.Compile(compilerInput, true, false);
    if(compilerOutput is null)
    {
        Console.WriteLine($"//COMPILATION ERROR// TEST #{test}");
        Console.WriteLine(compilerInput);
        return;
    }
    
    string vmoutput = VirtualMachine.Execute(compilerOutput, 16000);
    Analyzer.Check(vmoutput, out string? error);
    
    //if(error is not null)
    {
        Console.WriteLine($"//TEST #{test}//");
        Console.WriteLine("//AN ERROR FOUND//");
        Console.WriteLine(error);
        Console.WriteLine("//SOURCE CODE//");
        Console.WriteLine(compilerInput);
        Console.ReadKey();
        Console.WriteLine("//OUTPUT CODE//");
        Console.WriteLine(compilerOutput);
        Console.ReadKey();
        Console.WriteLine("//VM LOG//");
        Console.WriteLine(vmoutput);
    }

    return;
}

var end = System.DateTime.Now;
Console.WriteLine((end - begin).TotalSeconds);


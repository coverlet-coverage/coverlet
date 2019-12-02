#nullable enable

using System;
using System.IO;

namespace Calculator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || !File.Exists(args[0]))
            {
                Console.WriteLine("Instrumentation result file not found");
            }


            RealTimeCoverageAnalysis inProc = new RealTimeCoverageAnalysis(args[0]);
            CalculatorRuntime runtime = new CalculatorRuntime();
            double? operanda = null;
            double? operandb = null;
            for (; ; )
            {
                if (operanda is null)
                {
                    Console.WriteLine("Insert operand a");
                    double operand;
                    while (!double.TryParse(Console.ReadLine(), out operand))
                    {
                        Console.WriteLine("Invalid value, insert operand a");
                    }
                    operanda = operand;
                }
                if (operandb is null)
                {
                    Console.WriteLine("Insert operand b");
                    double operand;
                    while (!double.TryParse(Console.ReadLine(), out operand))
                    {
                        Console.WriteLine("Invalid value, insert operand b");
                    }
                    operandb = operand;
                }


                for (; ; )
                {
                    Console.WriteLine("Insert operation");
                    ConsoleKeyInfo consoleKeyInfo = Console.ReadKey();
                    if (consoleKeyInfo.Key != ConsoleKey.Add && consoleKeyInfo.Key != ConsoleKey.Subtract &&
                        consoleKeyInfo.Key != ConsoleKey.Divide && consoleKeyInfo.Key != ConsoleKey.Multiply)
                    {
                        Console.WriteLine("Invalid operation, allowed operation, +-*/");
                    }
                    else
                    {
                        Console.WriteLine();
                        switch (consoleKeyInfo.Key)
                        {
                            case ConsoleKey.Add:
                                {
                                    Console.WriteLine($"Result: {runtime.Add(operanda.Value, operandb.Value)}");
                                    break;
                                }
                            case ConsoleKey.Subtract:
                                {
                                    Console.WriteLine($"Result: {runtime.Subtrac(operanda.Value, operandb.Value)}");
                                    break;
                                }
                            case ConsoleKey.Multiply:
                                {
                                    Console.WriteLine($"Result: {runtime.Multiply(operanda.Value, operandb.Value)}");
                                    break;
                                }
                            case ConsoleKey.Divide:
                                {
                                    Console.WriteLine($"Result: {runtime.Divide(operanda.Value, operandb.Value)}");
                                    break;
                                }
                            default:
                                break;
                        }
                        operanda = operandb = null;

                        inProc.PrintCoverageCurrentState();

                        Console.WriteLine();
                        Console.WriteLine("Exit(press E)? Any other button to another loop");
                        if (Console.ReadKey().Key == ConsoleKey.E)
                        {
                            return;
                        }
                        Console.Clear();
                        break;
                    }
                }
            }
        }
    }
}


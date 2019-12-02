### Run sample

1) Go to root coverlet folder and generate packages
```
dotnet pack
...
Successfully created package 'C:\git\coverlet\bin\Debug\Packages\coverlet.core.1.0.4-gac3e48b424.nupkg'
```
2) Update `run_instrumentor.cmd` with correct package version(in this case *1.0.4-gac3e48b424*)
```
...
dotnet build /p:coverletCoreVersion=1.0.4-gac3e48b424
...
```
3) Open fresh new command line and run `run_instrumentor.cmd`
```
run_instrumentor.cmd

dotnet build /p:coverletCoreVersion=1.0.2-g1df3e82a5b
Microsoft (R) Build Engine version 16.2.32702+c4012a063 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 54,96 ms for C:\git\coverlet\Documentation\Examples\StandaloneUsage\Calculator\Calculator.csproj.
  Restore completed in 54,96 ms for C:\git\coverlet\Documentation\Examples\StandaloneUsage\Instrumentor\Instrumentor.csproj.
  Instrumentor -> C:\git\coverlet\Documentation\Examples\StandaloneUsage\Instrumentor\bin\Debug\netcoreapp2.2\Instrumentor.dll
  Calculator -> C:\git\coverlet\Documentation\Examples\StandaloneUsage\Calculator\bin\Debug\netcoreapp2.2\Calculator.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.25

dotnet Instrumentor/bin/Debug/netcoreapp2.2/Instrumentor.dll Calculator/bin/Debug/netcoreapp2.2/Calculator.dll
[LogVerbose] Instrumented module: 'Calculator\bin\Debug\netcoreapp2.2\Calculator.dll'
Instrumentation result saved 'C:\git\coverlet\Documentation\Examples\StandaloneUsage\instrumentationResult'
Instrumentor active, click any button to restore instrumented libraries
```
This process instruments "Calculator" app dll, you need to keep this process alive until end of app usage because when 
instrumentor process shutdown coverlet restores old "non instrumented" libraries.

4) Open another fresh new command line and run `run_app.cmd` and play with it
```
run_app.cmd

dotnet Calculator/bin/Debug/netcoreapp2.2/Calculator.dll instrumentationResult
Insert operand a
10
Insert operand b
20
Insert operation
+
Result: 30

***Start live coverage analysis***
---List of instrumented assemblies---
Calculator, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
---Method lines coverage---
Method 'System.Double Calculator.CalculatorRuntime::Add(System.Double,System.Double)' 100%
Method 'System.Double Calculator.CalculatorRuntime::Subtrac(System.Double,System.Double)' 0%
Method 'System.Double Calculator.CalculatorRuntime::Divide(System.Double,System.Double)' 0%
Method 'System.Double Calculator.CalculatorRuntime::Multiply(System.Double,System.Double)' 0%
Method 'System.Void Calculator.Program::Main(System.String[])' 100%
Method 'System.Void Calculator.RealTimeCoverageAnalysis::PrintCoverageCurrentState()' 100%
Method 'System.Void Calculator.RealTimeCoverageAnalysis::.ctor(System.String)' 100%
Total modules method lines covered '57,14'

Exit(press E)? Any other button to another loop
```
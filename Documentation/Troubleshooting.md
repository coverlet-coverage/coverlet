# Troubleshooting

## Msbuild Integration

1) Generate verbose log
```
dotnet test test\coverlet.core.tests\coverlet.core.tests.csproj -c debug /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Include=[coverlet.*]* -verbosity:diagnostic -bl:msbuild.binlog -noconsolelogger
```

2) Download http://msbuildlog.com/
3) Open `msbuild.binlog` generated and search for '[coverlet]' logs
 
![File](images/file.png)

## Coverlet Global Tool

```
coverlet "C:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\coverlet.core.tests.dll" --target "dotnet" --targetargs "test C:\git\coverlet\test\coverlet.core.tests --no-build" --verbosity detailed
```
Sample output
```
...
Instrumented module: 'C:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\coverlet.core.dll'
Instrumented module: 'C:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\xunit.runner.reporters.netcoreapp10.dll'
Instrumented module: 'C:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\xunit.runner.utility.netcoreapp10.dll'
Instrumented module: 'C:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\xunit.runner.visualstudio.dotnetcore.testadapter.dll'
Test run for C:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\coverlet.core.tests.dll(.NETCoreApp,Version=v2.0)
Microsoft (R) Test Execution Command Line Tool Version 16.0.1
Copyright (c) Microsoft Corporation.  All rights reserved.
Starting test execution, please wait...
[xUnit.net 00:00:01.4218329]     Coverlet.Core.Tests.Instrumentation.ModuleTrackerTemplateTests.HitsOnMultipleThreadsCorrectlyCounted [SKIP]
Skipped  Coverlet.Core.Tests.Instrumentation.ModuleTrackerTemplateTests.HitsOnMultipleThreadsCorrectlyCounted
[xUnit.net 00:00:03.6302618]     Coverlet.Core.Instrumentation.Tests.InstrumenterTests.TestCoreLibInstrumentation [SKIP]
Skipped  Coverlet.Core.Instrumentation.Tests.InstrumenterTests.TestCoreLibInstrumentation
Total tests: 113. Passed: 111. Failed: 0. Skipped: 2.
Test Run Successful.
Test execution time: 4,6411 Seconds

Calculating coverage result...
Hits file:'C:\Users\Marco\AppData\Local\Temp\coverlet.core_703263e9-21f0-4d1c-9ce3-98ddeacecc01' not found for module: 'coverlet.core'
  Generating report 'C:\git\coverlet\src\coverlet.console\bin\Debug\netcoreapp2.2\coverage.json'
+--------------------------------------------------+--------+--------+--------+
| Module                                           | Line   | Branch | Method |
+--------------------------------------------------+--------+--------+--------+
| coverlet.core                                    | 0%     | 0%     | 0%     |
+--------------------------------------------------+--------+--------+--------+
| xunit.runner.reporters.netcoreapp10              | 2,11%  | 0,83%  | 8,88%  |
+--------------------------------------------------+--------+--------+--------+
| xunit.runner.utility.netcoreapp10                | 12,06% | 6,6%   | 17,09% |
+--------------------------------------------------+--------+--------+--------+
| xunit.runner.visualstudio.dotnetcore.testadapter | 50,04% | 42,08% | 50,9%  |
+--------------------------------------------------+--------+--------+--------+

+---------+--------+--------+--------+
|         | Line   | Branch | Method |
+---------+--------+--------+--------+
| Total   | 17,76% | 13,82% | 22,64% |
+---------+--------+--------+--------+
| Average | 4,44%  | 3,455% | 5,66%  |
+---------+--------+--------+--------+
```

## Use local build

Sometimes is useful test local updated source to fix issue.  
You can "load" your local build using simple switch:

* build repo

```
D:\git\coverlet (fixjsonserializerbug -> origin)
λ dotnet build
Microsoft (R) Build Engine version 16.1.76+g14b0a930a7 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 52.23 ms for D:\git\coverlet\test\coverlet.testsubject\coverlet.testsubject.csproj.
  Restore completed in 58.97 ms for D:\git\coverlet\src\coverlet.console\coverlet.console.csproj.
  Restore completed in 59 ms for D:\git\coverlet\src\coverlet.core\coverlet.core.csproj.
  Restore completed in 59.17 ms for D:\git\coverlet\src\coverlet.msbuild.tasks\coverlet.msbuild.tasks.csproj.
  Restore completed in 59.26 ms for D:\git\coverlet\src\coverlet.collector\coverlet.collector.csproj.
  Restore completed in 60.1 ms for D:\git\coverlet\test\coverlet.collector.tests\coverlet.collector.tests.csproj.
  Restore completed in 60.42 ms for D:\git\coverlet\test\coverlet.core.performancetest\coverlet.core.performancetest.csproj.
  Restore completed in 60.47 ms for D:\git\coverlet\test\coverlet.core.tests\coverlet.core.tests.csproj.
  Restore completed in 22.85 ms for D:\git\coverlet\test\coverlet.core.tests\coverlet.core.tests.csproj.
  coverlet.testsubject -> D:\git\coverlet\test\coverlet.testsubject\bin\Debug\netcoreapp2.0\coverlet.testsubject.dll
  coverlet.core -> D:\git\coverlet\src\coverlet.core\bin\Debug\netstandard2.0\coverlet.core.dll
  coverlet.msbuild.tasks -> D:\git\coverlet\src\coverlet.msbuild.tasks\bin\Debug\netstandard2.0\coverlet.msbuild.tasks.dll
  coverlet.collector -> D:\git\coverlet\src\coverlet.collector\bin\Debug\netcoreapp2.0\coverlet.collector.dll
  coverlet.console -> D:\git\coverlet\src\coverlet.console\bin\Debug\netcoreapp2.2\coverlet.console.dll
  coverlet.core.performancetest -> D:\git\coverlet\test\coverlet.core.performancetest\bin\Debug\netcoreapp2.0\coverlet.core.performancetest.dll
  coverlet.core.tests -> D:\git\coverlet\test\coverlet.core.tests\bin\Debug\netcoreapp2.0\coverlet.core.tests.dll
  coverlet.collector.tests -> D:\git\coverlet\test\coverlet.collector.tests\bin\Debug\netcoreapp2.2\coverlet.collector.tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:07.42

D:\git\coverlet (fixjsonserializerbug -> origin)
λ

```

* Go to repro project and run
```
D:\git\Cake.Codecov\Source\Cake.Codecov.Tests (develop -> origin)
λ dotnet test /p:CollectCoverage=true /p:Exclude="[xunit.*]*" /p:CoverletToolsPath=D:\git\coverlet\src\coverlet.msbuild.tasks\bin\Debug\netstandard2.0\  
Test run for D:\git\Cake.Codecov\Source\Cake.Codecov.Tests\bin\Debug\netcoreapp2.0\Cake.Codecov.Tests.dll(.NETCoreApp,Version=v2.0)
...
```

In this way you can add `Debug.Launch()` inside coverlet source and debug.
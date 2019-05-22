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

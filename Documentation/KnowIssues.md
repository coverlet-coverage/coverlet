# Know Issues

## 1) VSTest stops process execution early(`dotnet test`)  

*Affected drivers*: msbuild(`dotnet test`) , dotnet tool(if you're using ` --targetargs "test ... --no-build"`)  

 *Symptoms:* 
 * warning or error like

 `Unable to read beyond end of stream`  

 `warning : [coverlet] Hits file:'C:\Users\REDACTED\AppData\Local\Temp\testApp_ac32258b-fd4a-4bb4-824c-a79061e97c31' not found for module: 'testApp'`

 *  zero coverage result (often only on CI but not on local)  
```
Calculating coverage result...
C:\Users\REDACTED\.nuget\packages\coverlet.msbuild\2.6.0\build\netstandard2.0\coverlet.msbuild.targets(21,5): warning : [coverlet] Hits file:'C:\Users\REDACTED\AppData\Local\Temp\testApp_ac32258b-fd4a-4bb4-824c-a79061e97c31' not found for module: 'testApp' [C:\Users\REDACTED\Documents\repo\testapp\testapp.Tests\testapp.Tests.csproj]
C:\Users\REDACTED\.nuget\packages\coverlet.msbuild\2.6.0\build\netstandard2.0\coverlet.msbuild.targets(21,5): warning : [coverlet] Hits file:'C:\Users\REDACTED\AppData\Local\Temp\testApp.Tests_ac32258b-fd4a-4bb4-824c-a79061e97c31' not found for module: 'testApp.Tests' [C:\Users\REDACTED\Documents\repo\testapp\testapp.Tests\testapp.Tests.csproj]
  Generating report 'C:\Users\REDACTED\Documents\repo\testapp\lcov.info'

+---------------+------+--------+--------+
| Module        | Line | Branch | Method |
+---------------+------+--------+--------+
| testApp       | 0%   | 0%     | 0%     |
+---------------+------+--------+--------+
| testApp.Tests | 0%   | 100%   | 0%     |
+---------------+------+--------+--------+

+---------+------+--------+--------+
|         | Line | Branch | Method |
+---------+------+--------+--------+
| Total   | 0%   | 0%     | 0%     |
+---------+------+--------+--------+
| Average | 0%   | 0%     | 0%     |
+---------+------+--------+--------+
```

The issue is related to vstest platform https://github.com/microsoft/vstest/issues/1900#issuecomment-457488472  
```
However if testhost doesn't shut down within 100ms(as the execution is completed, we expect it to shutdown fast). vstest.console forcefully kills the process.
```

Coverlet collect and write hits data on process exist, if for some reason process is too slow to close will be killed and we cannot collect coverage result.
This happen also if there are other "piece of code" during testing that slow down process exit.
We found problem for instance with test that uses RabbitMQ.

*Solution:* 
The only way to solve this issue is to use collectors integration https://github.com/tonerdo/coverlet#vstest-integration.  
With collector we're injected in test host throught a in-proc collector that talk with vstest platform so we can signal when we end our work.  
Check requirements https://github.com/tonerdo/coverlet#requirements you need to run *.NET Core SDK v2.2.401 or newer*.

## 2) Upgrade `coverlet.collector` to version > 1.0.0

*Affected drivers*: vstest integration `dotnet test --collect:"XPlat Code Coverage"`  

 *Symptoms:* The same of know issue 1.  

There is a bug inside vstest platform https://github.com/microsoft/vstest/issues/2205.  
If you upgrade collector package with version greather than 1.0.0 in-proc collector won't be loaded so you could incur in know issue number 1 and get zero coverage result

*Solution:* you need to pass custom *runsetting* file like this
```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>cobertura</Format>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
  <InProcDataCollectionRunSettings>
    <InProcDataCollectors>
      <InProcDataCollector assemblyQualifiedName="Coverlet.Collector.DataCollection.CoverletInProcDataCollector, coverlet.collector, Version=1.1.0.0, Culture=neutral, PublicKeyToken=null"
                     friendlyName="XPlat Code Coverage"
                     enabled="True"
                     codebase="coverlet.collector.dll" />
    </InProcDataCollectors>
  </InProcDataCollectionRunSettings>
</RunSettings>
```
And pass it to command line
```
dotnet test --settings runsetting
```



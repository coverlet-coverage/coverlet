# Known Issues

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
The only way to solve this issue is to use collectors integration https://github.com/tonerdo/coverlet#vstest-integration-preferred-due-to-known-issue.  
With collector we're injected in test host through a in-proc collector that talk with vstest platform so we can signal when we end our work.  

## 2) Upgrade `coverlet.collector` to version > 1.0.0

*Affected drivers*: vstest integration `dotnet test --collect:"XPlat Code Coverage"`  

 *Symptoms:* The same as known issue 1.  

There is a bug inside vstest platform https://github.com/microsoft/vstest/issues/2205.  
If you upgrade collector package with version greater than 1.0.0 in-proc collector won't be loaded so you could incur known issue number 1 and get zero coverage result

*Solutions:*   
1) Reference `Mcrosoft.NET.Test.Sdk` with version *greater than* 16.4.0  
For instance
```xml
<ItemGroup>
  ...
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="16.5.0" />
  ...
</ItemGroup>
```
2) You can pass custom *runsetting* file like this
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
```bash
dotnet test --settings runsetting
```
## 3) Nerdbank.GitVersioning and `/p:UseSourceLink=true` option

*Affected drivers*: all drivers that support `/p:UseSourceLink=true`

 *Symptoms:* some tool like SonarSource doesn't work well see https://github.com/tonerdo/coverlet/issues/482

 `Nerdbank.GitVersioning` generates a version file on the fly but this file is not part of user solution and it's not commited to repo so the generated remote source file reference does not exit, i.e.
 ```
 ...
 <File uid="1" fullPath="https://raw.githubusercontent.com/iron9light/HOCON.Json/654d4ea8ec524f72027e2b2d324aad9acf80b710/src/Hocon.Json/obj/Release/netstandard2.0/Hocon.Json.Version.cs" />
 ...
 ```

 *Solution:* we can exclude `Nerdbank.GitVersioning` autogenerated file from instrumentation using filter
 ```bash
 /p:ExcludeByFile=\"**/*Json.Version.cs\"
 ```
## 4) Failed to resolve assembly during instrumentation

*Affected drivers*: all drivers

 *Symptoms:* during build/instrumentation you get exception like
 ```
 [coverlet] Unable to instrument module: ..\UnitTests\bin\Debug\netcoreapp2.1\Core.Messaging.dll because : Failed to resolve assembly: 'Microsoft.Azure.ServiceBus, Version=3.4.0.0, Culture=neutral, PublicKeyToken=7e34167dcc6d6d8c' [..\UnitTests.csproj]
 ```

 In the instrumentation phase coverlet needs to load all reference used by your instrumented module. Sometime the build phase(out of coverlet control) does not copy those dll to output target because references are resolved for instance at runtime or in publish phase from nuget packages folders.

 *Solution:* we need to tell to msbild to copy nuget dll reference to output using msbuild switch `CopyLocalLockFileAssemblies`
 ```bash
 dotnet test /p:CollectCoverage=true /p:CopyLocalLockFileAssemblies=true
 ```
 or adding the attribute `<CopyLocalLockFileAssemblies>` to project 
 file
 ```xml
 <PropertyGroup>
 ...
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
 ...
</PropertyGroup>
 ```
 NB. This **DOESN'T ALWAYS WORK**, for instance in case of shared framework https://github.com/dotnet/cli/issues/12705#issuecomment-536686785

 We can do nothing at the moment this is a build behaviour out of our control.  
 This issue should not happen for .net runtime version >= 3.0 because the new default behavior is copy all assets to the build output https://github.com/dotnet/cli/issues/12705#issuecomment-535150372  

 In this case the only workaround for the moment is to *manually copy* missing dll to output folder https://github.com/tonerdo/coverlet/issues/560#issue-496440052 "The only reliable way to work around this problem is to drop the DLL in the unit tests project's bin\Release\netcoreapp2.2 directory."






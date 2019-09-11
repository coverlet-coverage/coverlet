**Run from solution root sln**

To merge report togheter you need to run separate test and merge in one `json` format file.
Last command will join and create final needed format file.

```
dotnet test XUnitTestProject1\XUnitTestProject1.csproj /p:CollectCoverage=true  /p:CoverletOutput=../CoverageResults/ -l:trx;LogFileName=UnitTestOutput1.xml
dotnet test XUnitTestProject2\XUnitTestProject2.csproj /p:CollectCoverage=true  /p:CoverletOutput=../CoverageResults/ -l:trx;LogFileName=UnitTestOutput2.xml /p:MergeWith="../CoverageResults/coverage.json"
dotnet test XUnitTestProject3\XUnitTestProject3.csproj /p:CollectCoverage=true  /p:CoverletOutput=../CoverageResults/ -l:trx;LogFileName=UnitTestOutput3.xml /p:MergeWith="../CoverageResults/coverage.json" /p:CoverletOutputFormat="opencover"
```
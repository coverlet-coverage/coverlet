**Run from solution root sln**

To merge report togheter you need to run separate test and merge in one `json` format file.
Last command will join and create final needed format file.

```
dotnet test XUnitTestProject1\XUnitTestProject1.csproj /p:CollectCoverage=true  /p:CoverletOutput=../CoverageResults/
dotnet test XUnitTestProject2\XUnitTestProject2.csproj /p:CollectCoverage=true  /p:CoverletOutput=../CoverageResults/ /p:MergeWith="../CoverageResults/coverage.json"
dotnet test XUnitTestProject3\XUnitTestProject3.csproj /p:CollectCoverage=true  /p:CoverletOutput=../CoverageResults/ /p:MergeWith="../CoverageResults/coverage.json" /p:CoverletOutputFormat="opencover"
```

You can merge also running `dotnet test` and merge with single command from a solution file, but you need to ensure that tests will run sequentially(`-m:1`). This slow down testing but avoid invalid coverage result.

```
dotnet test /p:CollectCoverage=true  /p:CoverletOutput=../CoverageResults/ /p:MergeWith="../CoverageResults/coverage.json" /p:CoverletOutputFormat=\"opencover,json\" -m:1
```
N.B. You need to specify `json` format plus another format(the final one), because Coverlet can only merge proprietary format. At the end you can delete temporary `coverage.json` file.

You can also merge the coverage result and generate another valid format to export the content than opencover, like cobertura.

```
dotnet test /p:CollectCoverage=true  /p:CoverletOutput=../CoverageResults/ /p:MergeWith="../CoverageResults/coverage.json" /p:CoverletOutputFormat=\"cobertura,json\" -m:1
```

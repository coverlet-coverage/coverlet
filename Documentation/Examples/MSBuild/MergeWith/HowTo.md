# Merging Coverage Reports

## Running Tests Separately

To merge coverage reports, run tests for each project and combine them into a single file:

```bash
# Generate coverage for first project
dotnet test XUnitTestProject1/XUnitTestProject1.csproj \
    /p:CollectCoverage=true \
    /p:CoverletOutput=../CoverageResults/

# Merge coverage from second project
dotnet test XUnitTestProject2/XUnitTestProject2.csproj \
    /p:CollectCoverage=true \
    /p:CoverletOutput=../CoverageResults/ \
    /p:MergeWith="../CoverageResults/coverage.json"

# Merge coverage from third project and generate final OpenCover report
dotnet test XUnitTestProject3/XUnitTestProject3.csproj \
    /p:CollectCoverage=true \
    /p:CoverletOutput=../CoverageResults/ \
    /p:MergeWith="../CoverageResults/coverage.json" \
    /p:CoverletOutputFormat="opencover"
```

## Running Tests from Solution

To merge coverage using a single command (requires sequential execution):

```bash
dotnet test \
    /p:CollectCoverage=true \
    /p:CoverletOutput=../CoverageResults/ \
    /p:MergeWith="../CoverageResults/coverage.json" \
    /p:CoverletOutputFormat="opencover,json" \
    -m:1
```

> **Note**: Sequential execution (`-m:1`) ensures accurate coverage but increases test duration.

## Important Considerations

- Include `json` format alongside your desired output format
- Coverlet only merges its proprietary JSON format
- The temporary `coverage.json` file can be deleted after merging

## Example with Cobertura Output

```bash
dotnet test \
    /p:CollectCoverage=true \
    /p:CoverletOutput=../CoverageResults/ \
    /p:MergeWith="../CoverageResults/coverage.json" \
    /p:CoverletOutputFormat="cobertura,json" \
    -m:1
```

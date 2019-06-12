param (
    [string]$apiKey,
    [string]$source
 )

if (!$apiKey -or !$source)
{
     Write-Host -ForegroundColor Red Specify apiKey and source
     exit
}

Write-Host -ForegroundColor Blue Publish with .NET CLI
& dotnet --info

Write-Host -ForegroundColor Green Create Packages
& dotnet pack -c Release /p:PublicRelease=false

Write-Host -ForegroundColor Green Upload Packages
& dotnet nuget push "$PSScriptRoot\..\bin\Release\Packages\*.nupkg" -k $apiKey -s $source

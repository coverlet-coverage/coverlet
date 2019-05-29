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
& dotnet msbuild "$PSScriptRoot\..\build.proj" /t:CreateNuGetPackage /p:Configuration=Release /p:PublicRelease=false # amend build.proj path if changes

Write-Host -ForegroundColor Green Upload Packages
& dotnet nuget push "$PSScriptRoot\..\bin\Release\Packages\*.nupkg" -k $apiKey -s $source

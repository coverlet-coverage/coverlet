param (
    [string]$apiKey,
    [string]$source
 )

 if (!$apiKey -or !$source)
 {
     Write-Host -ForegroundColor Red Specify apiKey and source
 }

$configuration = "Release"
$reporoot = Resolve-Path "$PSScriptRoot\.."
$dotnetcli = "dotnet" # change with custom dowloaded cli

Write-Host -ForegroundColor Green Repo root $reporoot
Write-Host -ForegroundColor Blue Publish with .NET cli
& $dotnetcli --info

Write-Host Create Packages

& $dotnetcli msbuild "$PSScriptRoot\..\build.proj" /t:CreateNuGetPackage /p:Configuration=$configuration # amed build.proj path with future new position under eng

#& $dotnetcli nuget push "$reporoot\build\$configuration\*.nupkg" -k $apiKey -s $source
& nuget push -Source "TestCoverlet2" -ApiKey AzureDevOps "$reporoot\build\$configuration\*.nupkg" #test with my azure artifacts
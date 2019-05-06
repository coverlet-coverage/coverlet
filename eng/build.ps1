 param (
    [string]$configuration = 'Debug'
 )

function Create-Directory([string[]] $path) {
  if (!(Test-Path $path)) {
    New-Item -path $path -force -itemType "Directory" | Out-Null
  }
}

function DownloadRuntime 
{
    set-strictmode -version 2.0
    $ErrorActionPreference = "Stop"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12  

     $toolsRoot = Join-Path $PSScriptRoot "..\.tools"
     Create-Directory $toolsRoot
     $scriptPath = Join-Path $toolsRoot "dotnet-install.ps1"
     Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $scriptPath
     & $toolsRoot\dotnet-install.ps1 -InstallDir $toolsRoot\dotnet -Version 2.2.203
}

Write-Host -ForegroundColor Green Build in $configuration configuration

DownloadRuntime

$env:DOTNET_MULTILEVEL_LOOKUP=0
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1

Write-Host -ForegroundColor Blue "Build with dotnet driver:" 
Write-Host
& $PSScriptRoot\..\.tools\dotnet\dotnet.exe --info

& $PSScriptRoot\..\.tools\dotnet\dotnet.exe msbuild $PSScriptRoot\build.proj /p:Configuration=$configuration
[CmdletBinding(DefaultParameterSetName='RegularBuild')]
param (
    [ValidateSet("debug", "release")]
    [string]$Configuration = 'debug',
    [int]$BuildNumber,
    [switch]$OnlyUnitTests,
    [string]$SignedPackageDirectory = ''
)

# For TeamCity - If any issue occurs, this script fails the build. - By default, TeamCity returns an exit code of 0 for all powershell scripts, even if they fail
trap {
    Write-Host "BUILD FAILED: $_" -ForegroundColor Red
    Write-Host "ERROR DETAILS:" -ForegroundColor Red
    Write-Host $_.Exception -ForegroundColor Red
    Write-Host ("`r`n" * 3)
    exit 1
}

$CLIRoot= Join-Path $PSScriptRoot 'cli'
$env:DOTNET_INSTALL_DIR=$CLIRoot

. "$PSScriptRoot\build\common.ps1"

if ([string]::IsNullOrEmpty($SignedPackageDirectory)) {
    $env:SignedPackagePath = ""
}
else {
    $signedPackages = (Join-Path $SignedPackageDirectory "*.nupkg" | Get-ChildItem -Recurse)

    if ($signedPackages.Length -lt 1) {
        throw "Could not find any packages at path $($SignedPackageDirectory)"
    }

    $env:SignedPackagePath = $signedPackages[0].FullName
}

Trace-Log "Set signed package path to: '$($env:SignedPackagePath)'"
Trace-Log "DotNet CLI directory: $($env:DOTNET_INSTALL_DIR)"

Function Run-Tests {
    [CmdletBinding()]
    param()

    Trace-Log 'Running tests'

    $xUnitExe = (Join-Path $PSScriptRoot "packages\xunit.runner.console.2.2.0\tools\xunit.console.exe")

    $UnitTestAssemblies = @("test\NuGet.Services.EndToEnd.Test\bin\$Configuration\NuGet.Services.EndToEnd.Test.dll")

    $AllTestAssemblies = $UnitTestAssemblies + `
        @("src\NuGet.Services.EndToEnd\bin\$Configuration\NuGet.Services.EndToEnd.dll")

    if ($OnlyUnitTests) {
        $AllTestAssemblies = $UnitTestAssemblies
    }

    $TestCount = 0

    foreach ($TestAssembly in $AllTestAssemblies) {
        & $xUnitExe (Join-Path $PSScriptRoot $TestAssembly) -xml "TestResults.$TestCount.xml" -Verbose
        $TestCount++
    }
}

Write-Host ("`r`n" * 3)
Trace-Log ('=' * 60)

$startTime = [DateTime]::UtcNow
if (-not $BuildNumber) {
    $BuildNumber = Get-BuildNumber
}
Trace-Log "Build #$BuildNumber started at $startTime"

$TestErrors = @()

Invoke-BuildStep 'Running tests' { Run-Tests } `
    -ev +TestErrors

Trace-Log ('-' * 60)

## Calculating Build time
$endTime = [DateTime]::UtcNow
Trace-Log "Build #$BuildNumber ended at $endTime"
Trace-Log "Time elapsed $(Format-ElapsedTime ($endTime - $startTime))"

Trace-Log ('=' * 60)

if ($TestErrors) {
    $ErrorLines = $TestErrors | %{ ">>> $($_.Exception.Message)" }
    Error-Log "Tests completed with $($TestErrors.Count) error(s):`r`n$($ErrorLines -join "`r`n")" -Fatal
}

Write-Host ("`r`n" * 3)
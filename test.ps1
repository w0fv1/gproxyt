param([string] $DotnetPath = "")

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $projectRoot "GproxytWorkspace.psm1") -Force
$dotnet = if ([string]::IsNullOrWhiteSpace($DotnetPath)) {
    Resolve-GproxytDotnet -ProjectRoot $projectRoot
} else {
    [IO.Path]::GetFullPath($DotnetPath)
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
& $dotnet test (Join-Path $projectRoot "Gproxyt.slnx") -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Import-Module Pester -ErrorAction Stop
$pesterResult = Invoke-Pester (Join-Path $projectRoot "tests") -PassThru
if ($pesterResult.FailedCount -ne 0) {
    exit 1
}

param(
    [string] $OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $projectRoot "GproxytWorkspace.psm1") -Force
$repoRoot = Find-NfircoSuperprojectRoot -ProjectRoot $projectRoot
$localDotnet = if ($null -ne $repoRoot) { Join-Path $repoRoot ".tmp\dotnet\dotnet.exe" } else { $null }
$dotnet = if ($null -ne $localDotnet -and (Test-Path -LiteralPath $localDotnet -PathType Leaf)) {
    $localDotnet
} else {
    (Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
}
$publishDirectory = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $projectRoot "dist"
} else {
    [IO.Path]::GetFullPath($OutputDirectory)
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
& $dotnet test (Join-Path $projectRoot "Gproxyt.slnx") -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $dotnet publish (Join-Path $projectRoot "src\Gproxyt\Gproxyt.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    --nologo `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDirectory
exit $LASTEXITCODE

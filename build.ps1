param(
    [string] $OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $projectRoot "GproxytWorkspace.psm1") -Force
$dotnet = Resolve-GproxytDotnet -ProjectRoot $projectRoot
$publishDirectory = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $projectRoot "dist"
} else {
    [IO.Path]::GetFullPath($OutputDirectory)
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
& (Join-Path $projectRoot "test.ps1") -DotnetPath $dotnet
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

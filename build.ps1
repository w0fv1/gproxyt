param(
    [string] $OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $projectRoot "..\..")
$localDotnet = Join-Path $repositoryRoot ".tmp\dotnet\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $localDotnet) {
    $localDotnet
} else {
    (Get-Command dotnet -ErrorAction Stop).Source
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

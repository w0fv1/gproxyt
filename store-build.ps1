param(
    [string] $OutputDirectory = "",
    [string] $WinAppPath = "",
    [string] $CertificatePath = "",
    [string] $CertificatePassword = "password"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $projectRoot "GproxytWorkspace.psm1") -Force
Import-Module (Join-Path $projectRoot "ReleaseMetadata.psm1") -Force
Import-Module (Join-Path $projectRoot "StorePackaging.psm1") -Force
$dotnet = Resolve-GproxytDotnet -ProjectRoot $projectRoot
$winapp = Resolve-WinAppCli -Path $WinAppPath
$projectPath = Join-Path $projectRoot "src\Gproxyt\Gproxyt.csproj"
$manifestTemplatePath = Join-Path $projectRoot "packaging\Package.appxmanifest"
$logoPath = Join-Path $projectRoot "src\Gproxyt\Assets\gproxyt.png"
$semanticVersion = Read-GproxytProjectVersion -ProjectPath $projectPath
$packageVersion = ConvertTo-GproxytStoreVersion -Version $semanticVersion
$outputRoot = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $projectRoot "dist\store"
} else {
    [IO.Path]::GetFullPath($OutputDirectory)
}
$stagingRoot = Join-Path $projectRoot "obj\Store\$([Guid]::NewGuid().ToString('N'))"
$layoutPath = Join-Path $stagingRoot "layout"
$darkThemeLogoPath = Join-Path $stagingRoot "gproxyt-dark-theme.png"
$packagePath = Join-Path $outputRoot "GProxyT_$($packageVersion)_x64.msix"

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:WINAPP_CLI_TELEMETRY_OPTOUT = "1"
New-Item -ItemType Directory -Force -Path $layoutPath, $outputRoot | Out-Null
try {
    & (Join-Path $projectRoot "test.ps1") -DotnetPath $dotnet
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
    & $dotnet publish $projectPath `
        -c Release `
        -r win-x64 `
        --self-contained true `
        --nologo `
        -p:PublishSingleFile=false `
        -o $layoutPath
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
    Get-ChildItem -LiteralPath $layoutPath -Filter *.pdb -File | Remove-Item -Force
    Copy-Item -LiteralPath (Join-Path $layoutPath "gproxyt.exe") -Destination (Join-Path $layoutPath "gproxyt-startup.exe")
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestTemplatePath
    if (([regex]::Matches($manifest, 'Version="0\.0\.0\.0"')).Count -ne 1) {
        throw "Package manifest must contain exactly one version sentinel"
    }
    $manifest = $manifest.Replace('Version="0.0.0.0"', "Version=`"$packageVersion`"")
    $manifestPath = Join-Path $layoutPath "Package.appxmanifest"
    Set-Content -LiteralPath $manifestPath -Value $manifest -Encoding UTF8 -NoNewline
    New-GproxytDarkThemeLogo -SourcePath $logoPath -OutputPath $darkThemeLogoPath
    & $winapp manifest update-assets $darkThemeLogoPath --light-image $logoPath --manifest $manifestPath
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
    if (Test-Path -LiteralPath $packagePath -PathType Leaf) {
        Remove-Item -LiteralPath $packagePath -Force
    }
    $packageArguments = @(
        "package",
        $layoutPath,
        "--manifest",
        $manifestPath,
        "--output",
        $packagePath
    )
    if (-not [string]::IsNullOrWhiteSpace($CertificatePath)) {
        $packageArguments += @(
            "--cert",
            [IO.Path]::GetFullPath($CertificatePath),
            "--cert-password",
            $CertificatePassword
        )
    }
    & $winapp @packageArguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
    Get-Item -LiteralPath $packagePath | Select-Object FullName, Length, LastWriteTime
}
finally {
    if (Test-Path -LiteralPath $stagingRoot -PathType Container) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}

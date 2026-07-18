function ConvertTo-GproxytStoreVersion {
    param([Parameter(Mandatory)] [string] $Version)

    if ($Version -notmatch '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*$') {
        throw "Gproxyt.csproj must declare a semantic Version"
    }
    return "$($Matches.major).$($Matches.minor).$($Matches.patch).0"
}

function Resolve-WinAppCli {
    param([string] $Path = "")

    if (-not [string]::IsNullOrWhiteSpace($Path)) {
        $resolved = [IO.Path]::GetFullPath($Path)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "WinApp CLI not found at $resolved"
        }
        return $resolved
    }
    $command = Get-Command winapp -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $command) {
        throw "WinApp CLI is required. Install it with: winget install --id Microsoft.WinAppCli --exact --source winget"
    }
    return $command.Source
}

function Set-GproxytManifestLanguages {
    param(
        [Parameter(Mandatory)] [string] $ManifestPath,
        [Parameter(Mandatory)] [string] $ResourcesPath
    )

    $resolvedManifestPath = [IO.Path]::GetFullPath($ManifestPath)
    $resolvedResourcesPath = [IO.Path]::GetFullPath($ResourcesPath)
    $cultures = @(Get-ChildItem -LiteralPath $resolvedResourcesPath -Filter "Strings.*.json" -File |
        ForEach-Object { $_.BaseName.Substring("Strings.".Length) } |
        Sort-Object -Unique)
    if ($cultures.Count -eq 0) {
        throw "At least one localization resource is required"
    }
    if ($cultures -notcontains "en-US") {
        throw "The English fallback localization resource is required"
    }
    foreach ($culture in $cultures) {
        [Globalization.CultureInfo]::GetCultureInfo($culture) | Out-Null
    }

    [xml] $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $resolvedManifestPath
    $resources = $manifest.Package.Resources
    if ($null -eq $resources) {
        throw "Package manifest must contain a Resources element"
    }
    $resources.RemoveAll()
    foreach ($culture in $cultures) {
        $resource = $manifest.CreateElement("Resource", $manifest.Package.NamespaceURI)
        $resource.SetAttribute("Language", $culture)
        $resources.AppendChild($resource) | Out-Null
    }

    $settings = New-Object Xml.XmlWriterSettings
    $settings.Encoding = New-Object Text.UTF8Encoding $false
    $settings.Indent = $true
    $settings.OmitXmlDeclaration = $false
    $writer = [Xml.XmlWriter]::Create($resolvedManifestPath, $settings)
    try {
        $manifest.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}

Export-ModuleMember -Function ConvertTo-GproxytStoreVersion, Resolve-WinAppCli, Set-GproxytManifestLanguages

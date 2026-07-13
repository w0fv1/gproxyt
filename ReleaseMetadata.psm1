function Read-GproxytProjectVersion {
    param([Parameter(Mandatory)] [string] $ProjectPath)

    [xml] $project = Get-Content -Raw -Path $ProjectPath
    $version = [string]$project.Project.PropertyGroup.Version | Select-Object -First 1
    if ($version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
        throw "Gproxyt.csproj must declare a semantic Version"
    }
    return $version
}

function Read-GproxytReleaseReadme {
    param(
        [Parameter(Mandatory)] [string] $ReadmePath,
        [Parameter(Mandatory)] [string] $Version
    )

    $readme = (Get-Content -Raw -Encoding UTF8 -Path $ReadmePath).TrimStart([char]0xFEFF).Trim()
    $frontMatter = [regex]::Match($readme, '(?s)\A---\s*\r?\n(?<yaml>.*?)\r?\n---')
    if (-not $frontMatter.Success) {
        throw "README must start with YAML front matter"
    }
    $declaredVersion = [regex]::Match($frontMatter.Groups['yaml'].Value, '(?m)^\s*version\s*:\s*[''"]?(?<version>[^''"\r\n]+?)[''"]?\s*$')
    if (-not $declaredVersion.Success) {
        throw "README front matter must declare version"
    }
    if ($declaredVersion.Groups['version'].Value.Trim() -ne $Version) {
        throw "README version does not match project version $Version"
    }
    return $readme
}

function New-GproxytReleaseMetadata {
    param(
        [Parameter(Mandatory)] [string] $ArtifactPath,
        [Parameter(Mandatory)] [string] $Version,
        [Parameter(Mandatory)] [string] $Readme
    )

    $file = Get-Item -Path $ArtifactPath
    if ($file.Name -ne 'gproxyt.exe') {
        throw "Release artifact must be named gproxyt.exe"
    }
    return [ordered]@{
        appKey = 'gproxyt'
        platform = 'windows-x64'
        version = $Version
        fileName = $file.Name
        fileSizeBytes = $file.Length
        sha256 = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        sha512 = (Get-FileHash -Path $file.FullName -Algorithm SHA512).Hash.ToLowerInvariant()
        mimeType = 'application/vnd.microsoft.portable-executable'
        readme = $Readme
        access = 'PUBLIC'
    }
}

Export-ModuleMember -Function Read-GproxytProjectVersion, Read-GproxytReleaseReadme, New-GproxytReleaseMetadata

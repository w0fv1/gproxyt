$projectRoot = Split-Path -Parent $PSScriptRoot
$modulePath = Join-Path $projectRoot "ReleaseMetadata.psm1"
Import-Module $modulePath -Force

Describe "gproxyt release metadata" {
    It "reads the version from the project" {
        $projectPath = Join-Path $TestDrive "Gproxyt.csproj"
        Set-Content -Path $projectPath -Value '<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup></Project>'

        Read-GproxytProjectVersion -ProjectPath $projectPath | Should Be "1.0.0"
    }

    It "compresses the self-contained single file" {
        [xml] $project = Get-Content -Raw -Path (Join-Path $projectRoot "src\Gproxyt\Gproxyt.csproj")

        [string]$project.Project.PropertyGroup.EnableCompressionInSingleFile | Should Be "true"
    }

    It "requires the README version to match" {
        $readmePath = Join-Path $TestDrive "README.md"
        Set-Content -Path $readmePath -Value "---`nversion: 1.0.1`n---`n# gproxyt"

        $message = ""
        try {
            Read-GproxytReleaseReadme -ReadmePath $readmePath -Version "1.0.0"
        } catch {
            $message = $_.Exception.Message
        }

        $message | Should Match "does not match"
    }

    It "creates the public Windows executable contract" {
        $artifactPath = Join-Path $TestDrive "gproxyt.exe"
        [IO.File]::WriteAllBytes($artifactPath, [byte[]](1, 2, 3, 4))

        $metadata = New-GproxytReleaseMetadata -ArtifactPath $artifactPath -Version "1.0.0" -Readme "release"

        $metadata.appKey | Should Be "gproxyt"
        $metadata.platform | Should Be "windows-x64"
        $metadata.fileName | Should Be "gproxyt.exe"
        $metadata.fileSizeBytes | Should Be 4
        $metadata.mimeType | Should Be "application/vnd.microsoft.portable-executable"
        $metadata.access | Should Be "PUBLIC"
        $metadata.sha256 | Should Be ((Get-FileHash $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant())
        $metadata.sha512 | Should Be ((Get-FileHash $artifactPath -Algorithm SHA512).Hash.ToLowerInvariant())
    }

    It "documents the public release page" {
        $readme = Get-Content -Raw -Path (Join-Path $projectRoot "README.md")
        $releaseScript = Get-Content -Raw -Path (Join-Path $projectRoot "release.ps1")

        $readme | Should Match ([regex]::Escape("https://next.firco.cn/release/gproxyt"))
        $releaseScript | Should Match ([regex]::Escape('$baseUrl/release/$($metadata.appKey)'))
    }
}

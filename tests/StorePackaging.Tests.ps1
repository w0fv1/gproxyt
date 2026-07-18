$projectRoot = Split-Path -Parent $PSScriptRoot
$modulePath = Join-Path $projectRoot "StorePackaging.psm1"
Import-Module $modulePath -Force

Describe "GProxyT Store packaging" {
    It "derives the numeric Store package version from the project version" {
        ConvertTo-GproxytStoreVersion -Version "1.2.0-stable" | Should Be "1.2.0.0"
    }

    It "rejects an invalid project version" {
        $message = ""
        try {
            ConvertTo-GproxytStoreVersion -Version "1.2"
        }
        catch {
            $message = $_.Exception.Message
        }

        $message | Should Match "semantic Version"
    }

    It "keeps Store identity in the manifest template" {
        [xml] $manifest = Get-Content -Raw -Path (Join-Path $projectRoot "packaging\Package.appxmanifest")

        $manifest.Package.Identity.Name | Should Be "LaiqiInfo.GProxyT"
        $manifest.Package.Identity.Publisher | Should Be "CN=F5A5F8E6-B2BB-4B41-9DFE-7079CA6B44A4"
        $manifest.Package.Properties.PublisherDisplayName | Should Be "LaiqiInfo"
        $manifest.Package.Identity.Version | Should Be "0.0.0.0"
    }

    It "derives Store package languages from localization resources" {
        $manifestPath = Join-Path $TestDrive "Package.appxmanifest"
        $resourcesPath = Join-Path $projectRoot "src\Gproxyt\Resources"
        Copy-Item -LiteralPath (Join-Path $projectRoot "packaging\Package.appxmanifest") -Destination $manifestPath

        Set-GproxytManifestLanguages -ManifestPath $manifestPath -ResourcesPath $resourcesPath

        $expected = @(Get-ChildItem -LiteralPath $resourcesPath -Filter "Strings.*.json" -File |
            ForEach-Object { $_.BaseName.Substring("Strings.".Length) } |
            Sort-Object)
        [xml] $manifest = Get-Content -Raw -LiteralPath $manifestPath
        $actual = @($manifest.Package.Resources.Resource | ForEach-Object { $_.Language })

        $actual.Count | Should Be 20
        ($actual -join ",") | Should Be ($expected -join ",")
    }

    It "generates Store package languages before image assets" {
        $storeBuild = Get-Content -Raw -Path (Join-Path $projectRoot "store-build.ps1")

        $storeBuild | Should Match 'Set-GproxytManifestLanguages -ManifestPath \$manifestPath -ResourcesPath \$resourcesPath'
        $storeBuild.IndexOf("Set-GproxytManifestLanguages") | Should BeLessThan $storeBuild.IndexOf("manifest update-assets")
    }

    It "uses the complete brand image for light and dark Store assets" {
        $storeBuild = Get-Content -Raw -Path (Join-Path $projectRoot "store-build.ps1")

        $storeBuild | Should Match 'manifest update-assets \$logoPath --light-image \$logoPath'
        $storeBuild | Should Not Match 'New-GproxytDarkThemeLogo'
        (Get-Command New-GproxytDarkThemeLogo -ErrorAction SilentlyContinue) | Should BeNullOrEmpty
    }

    It "provides one square PNG source and a multi-size Windows icon" {
        Add-Type -AssemblyName System.Drawing
        $assetsPath = Join-Path $projectRoot "src\Gproxyt\Assets"
        $pngPath = Join-Path $assetsPath "gproxyt.png"
        $iconPath = Join-Path $assetsPath "gproxyt.ico"
        $image = [System.Drawing.Image]::FromFile($pngPath)
        try {
            $image.Width | Should Be 1024
            $image.Height | Should Be 1024
        }
        finally {
            $image.Dispose()
        }

        $stream = [IO.File]::OpenRead($iconPath)
        $reader = New-Object IO.BinaryReader $stream
        try {
            $reader.ReadUInt16() | Should Be 0
            $reader.ReadUInt16() | Should Be 1
            $entryCount = $reader.ReadUInt16()
            $entryCount | Should BeGreaterThan 7
            $sizes = @()
            for ($index = 0; $index -lt $entryCount; $index++) {
                $width = $reader.ReadByte()
                $reader.ReadByte() | Out-Null
                $reader.ReadBytes(14) | Out-Null
                $sizes += $(if ($width -eq 0) { 256 } else { $width })
            }
            ($sizes -contains 16) | Should Be $true
            ($sizes -contains 32) | Should Be $true
            ($sizes -contains 48) | Should Be $true
            ($sizes -contains 256) | Should Be $true
        }
        finally {
            $reader.Dispose()
            $stream.Dispose()
        }
    }

    It "provides one valid desktop screenshot without third-party product tiles" {
        Add-Type -AssemblyName System.Drawing
        $screenshots = @(Get-ChildItem -Path (Join-Path $projectRoot "packaging\store-assets\zh-CN") -Filter "*.png")

        $screenshots.Count | Should Be 1
        $screenshots[0].Name | Should Be "01-main-light.png"
        foreach ($screenshot in $screenshots) {
            $image = [System.Drawing.Image]::FromFile($screenshot.FullName)
            try {
                $image.Width | Should BeGreaterThan 1365
                $image.Height | Should BeGreaterThan 767
            }
            finally {
                $image.Dispose()
            }
        }
    }
}

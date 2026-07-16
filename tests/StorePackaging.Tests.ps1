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

    It "derives a dark-theme logo from the single brand source" {
        Add-Type -AssemblyName System.Drawing
        $sourcePath = Join-Path $TestDrive "source.png"
        $outputPath = Join-Path $TestDrive "dark-theme.png"
        $source = New-Object System.Drawing.Bitmap 2, 1
        try {
            $source.SetPixel(0, 0, [System.Drawing.Color]::FromArgb(255, 0, 0, 0))
            $source.SetPixel(1, 0, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            $source.Save($sourcePath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $source.Dispose()
        }

        New-GproxytDarkThemeLogo -SourcePath $sourcePath -OutputPath $outputPath

        $output = New-Object System.Drawing.Bitmap $outputPath
        try {
            $opaquePixel = $output.GetPixel(0, 0)
            $transparentPixel = $output.GetPixel(1, 0)
            $opaquePixel.A | Should Be 255
            $opaquePixel.R | Should Be 255
            $opaquePixel.G | Should Be 255
            $opaquePixel.B | Should Be 255
            $transparentPixel.A | Should Be 0
        }
        finally {
            $output.Dispose()
        }
    }

    It "provides four valid desktop screenshots" {
        Add-Type -AssemblyName System.Drawing
        $screenshots = @(Get-ChildItem -Path (Join-Path $projectRoot "packaging\store-assets\zh-CN") -Filter "*.png")

        $screenshots.Count | Should Be 4
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

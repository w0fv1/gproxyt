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
}

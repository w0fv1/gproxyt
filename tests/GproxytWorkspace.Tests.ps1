$projectRoot = Split-Path -Parent $PSScriptRoot
$modulePath = Join-Path $projectRoot "GproxytWorkspace.psm1"
Import-Module $modulePath -Force

Describe "gproxyt workspace discovery" {
    It "resolves the Nfirco Git superproject" {
        $root = Find-NfircoSuperprojectRoot -ProjectRoot $projectRoot

        Test-Path -LiteralPath (Join-Path $root "script\NfircoBackendApiCredential.psm1") -PathType Leaf | Should Be $true
    }

    It "does not trust an arbitrary filesystem ancestor" {
        $parent = Join-Path $TestDrive "parent"
        $standalone = Join-Path $parent "standalone"
        New-Item -ItemType Directory -Path (Join-Path $parent "script") -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $parent "script\NfircoBackendApiCredential.psm1") -Force | Out-Null
        & git init --quiet $standalone
        if ($LASTEXITCODE -ne 0) {
            throw "git init failed"
        }

        Find-NfircoSuperprojectRoot -ProjectRoot $standalone | Should Be $null
    }
}

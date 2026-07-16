function Find-NfircoSuperprojectRoot {
    param([Parameter(Mandatory)] [string] $ProjectRoot)

    $git = Get-Command git -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $git) {
        return $null
    }

    $current = [IO.Path]::GetFullPath($ProjectRoot)
    while ($true) {
        $output = & $git.Source -C $current rev-parse --show-superproject-working-tree 2>$null
        if ($LASTEXITCODE -ne 0) {
            return $null
        }

        $superproject = [string]($output | Select-Object -First 1)
        if ([string]::IsNullOrWhiteSpace($superproject)) {
            return $null
        }

        $candidate = [IO.Path]::GetFullPath($superproject.Trim())
        if ([string]::Equals($candidate, $current, [StringComparison]::OrdinalIgnoreCase)) {
            return $null
        }
        if (Test-Path -LiteralPath (Join-Path $candidate "script\NfircoBackendApiCredential.psm1") -PathType Leaf) {
            return $candidate
        }
        $current = $candidate
    }
}

function Resolve-GproxytDotnet {
    param([Parameter(Mandatory)] [string] $ProjectRoot)

    $repoRoot = Find-NfircoSuperprojectRoot -ProjectRoot $ProjectRoot
    $localDotnet = if ($null -ne $repoRoot) { Join-Path $repoRoot ".tmp\dotnet\dotnet.exe" } else { $null }
    if ($null -ne $localDotnet -and (Test-Path -LiteralPath $localDotnet -PathType Leaf)) {
        return $localDotnet
    }
    return (Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
}

Export-ModuleMember -Function Find-NfircoSuperprojectRoot, Resolve-GproxytDotnet

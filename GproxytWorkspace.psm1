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

Export-ModuleMember -Function Find-NfircoSuperprojectRoot

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

function New-GproxytDarkThemeLogo {
    param(
        [Parameter(Mandatory)] [string] $SourcePath,
        [Parameter(Mandatory)] [string] $OutputPath
    )

    Add-Type -AssemblyName System.Drawing
    $source = New-Object System.Drawing.Bitmap ([IO.Path]::GetFullPath($SourcePath))
    $output = New-Object System.Drawing.Bitmap $source.Width, $source.Height
    try {
        for ($y = 0; $y -lt $source.Height; $y++) {
            for ($x = 0; $x -lt $source.Width; $x++) {
                $alpha = $source.GetPixel($x, $y).A
                $output.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, 255, 255, 255))
            }
        }
        $output.Save([IO.Path]::GetFullPath($OutputPath), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $output.Dispose()
        $source.Dispose()
    }
}

Export-ModuleMember -Function ConvertTo-GproxytStoreVersion, Resolve-WinAppCli, New-GproxytDarkThemeLogo

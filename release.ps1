param(
    [string] $AppDomain = "next.firco.cn",
    [string] $BaseUrl = "",
    [string] $CredentialProfile = "prod"
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot
Import-Module (Join-Path $scriptRoot "GproxytWorkspace.psm1") -Force
$repoRoot = Find-NfircoSuperprojectRoot -ProjectRoot $scriptRoot
if ($null -eq $repoRoot) {
    throw "gproxyt release requires an Nfirco superproject checkout; standalone checkouts support build.ps1 only"
}
$releaseDirectory = Join-Path $repoRoot ".tmp\gproxyt-release"
$artifactPath = Join-Path $releaseDirectory "gproxyt.exe"
$verificationPath = Join-Path $releaseDirectory "verified-gproxyt.exe"
$projectPath = Join-Path $scriptRoot "src\Gproxyt\Gproxyt.csproj"
$readmePath = Join-Path $scriptRoot "README.md"

Import-Module (Join-Path $scriptRoot "ReleaseMetadata.psm1") -Force
Import-Module (Join-Path $repoRoot "script\NfircoBackendApiCredential.psm1") -Force

function Write-Step {
    param([Parameter(Mandatory)] [string] $Text)
    Write-Host "[gproxyt release] $Text"
}

function Invoke-NfircoApi {
    param(
        [Parameter(Mandatory)] [string] $Uri,
        [Parameter(Mandatory)] [object] $Body,
        [Parameter(Mandatory)] [hashtable] $Headers
    )

    $response = Invoke-RestMethod -Uri $Uri -Method Post -Headers $Headers -ContentType "application/json; charset=utf-8" -Body ($Body | ConvertTo-Json -Depth 8) -TimeoutSec 60 -NoProxy
    if ($null -eq $response) {
        throw "Nfirco API returned an empty response"
    }
    if ($response.isf) {
        throw "Nfirco API failed: $($response.msg)"
    }
    return $response.data
}

function Assert-PublishedMetadata {
    param(
        [Parameter(Mandatory)] [object] $Actual,
        [Parameter(Mandatory)] [Collections.IDictionary] $Expected
    )

    foreach ($property in @('appKey', 'platform', 'version', 'fileName', 'fileSizeBytes', 'sha256', 'sha512', 'readme')) {
        if ([string]$Actual.$property -ne [string]$Expected[$property]) {
            throw "Published metadata mismatch: $property"
        }
    }
}

$version = Read-GproxytProjectVersion -ProjectPath $projectPath
$readme = New-GproxytReleaseReadme -ReadmePath $readmePath -Version $version
$baseUrl = if ([string]::IsNullOrWhiteSpace($BaseUrl)) { "https://$AppDomain" } else { $BaseUrl }
$baseUrl = $baseUrl.TrimEnd('/')

Write-Step "构建版本 $version"
& (Join-Path $scriptRoot "build.ps1") -OutputDirectory $releaseDirectory
if ($LASTEXITCODE -ne 0) {
    throw "gproxyt build failed with exit code $LASTEXITCODE"
}

$metadata = New-GproxytReleaseMetadata -ArtifactPath $artifactPath -Version $version -Readme $readme
$credential = Read-NfircoBackendApiCredential -RepoRoot $repoRoot -Profile $CredentialProfile
$authorization = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("$($credential.Username):$($credential.Password)"))
$headers = @{ Authorization = "Basic $authorization" }
$createUri = "$baseUrl/apim/download/release/$($metadata.appKey)"
$completeUri = "$baseUrl/apim/download/release/$($metadata.appKey)/$version/complete"

Write-Step "发布文件 $artifactPath"
Write-Step "SHA256 $($metadata.sha256)"
$upload = Invoke-NfircoApi -Uri $createUri -Body $metadata -Headers $headers
if ($null -eq $upload -or [string]::IsNullOrWhiteSpace($upload.uploadUrl)) {
    throw "Nfirco API did not return uploadUrl"
}

Write-Step "上传到 OSS"
& curl.exe --fail --show-error --location --noproxy "*" --http1.1 --request PUT --header "Content-Type: $($metadata.mimeType)" --upload-file $artifactPath $upload.uploadUrl
if ($LASTEXITCODE -ne 0) {
    throw "OSS upload failed with curl exit code $LASTEXITCODE"
}

Write-Step "登记发布完成"
Invoke-NfircoApi -Uri $completeUri -Body @{ platform = $metadata.platform } -Headers $headers | Out-Null

Write-Step "校验线上元数据"
$latestUri = "$baseUrl/api/download/release/$($metadata.appKey)/latest?platform=$([uri]::EscapeDataString($metadata.platform))"
$latest = Invoke-RestMethod -Uri $latestUri -Method Get -TimeoutSec 60 -NoProxy
if ($null -eq $latest -or $latest.isf -or $null -eq $latest.data) {
    throw "Latest release metadata is unavailable"
}
Assert-PublishedMetadata -Actual $latest.data -Expected $metadata

Write-Step "回下载发布文件并校验哈希"
$downloadUri = "$baseUrl/api/download/release/$($metadata.appKey)/latest/file?platform=$([uri]::EscapeDataString($metadata.platform))"
Invoke-WebRequest -Uri $downloadUri -Method Get -OutFile $verificationPath -TimeoutSec 900 -NoProxy | Out-Null
$downloadSha256 = (Get-FileHash -Path $verificationPath -Algorithm SHA256).Hash.ToLowerInvariant()
$downloadSha512 = (Get-FileHash -Path $verificationPath -Algorithm SHA512).Hash.ToLowerInvariant()
if ($downloadSha256 -ne $metadata.sha256 -or $downloadSha512 -ne $metadata.sha512) {
    throw "Downloaded release hash mismatch"
}

$releasePageUrl = "$baseUrl/release/$($metadata.appKey)"
Write-Step "校验公开发布页面"
$releasePage = Invoke-WebRequest -Uri $releasePageUrl -Method Get -TimeoutSec 60 -NoProxy
if ([int]$releasePage.StatusCode -ne 200) {
    throw "Public release page is unavailable"
}

Write-Step "发布验证完成: $releasePageUrl"

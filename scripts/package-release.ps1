param(
    [Parameter(Mandatory=$true)][string]$ClientDir,
    [Parameter(Mandatory=$true)][string]$ServerDir,
    [Parameter(Mandatory=$true)][string]$OutputDir,
    [Parameter(Mandatory=$true)][string]$SourceCommit,
    [string]$Version="v3.0.0-coop-rc1",
    [string]$DependencyDir=""
)
$ErrorActionPreference="Stop"
Set-StrictMode -Version Latest

# Only HKMP binaries and redistributable libraries: never bundle Hollow Knight game DLLs.
function Find-RequiredFile([string]$name, [string[]]$roots) {
    foreach ($root in $roots) {
        $candidate=Join-Path $root $name
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    }
    throw "Required file missing: $name. Searched: $($roots -join ', '). Pass -DependencyDir with the required dependencies."
}
if ($SourceCommit -notmatch '^[a-fA-F0-9]{40}$') { throw "SourceCommit must be a 40-character Git SHA." }
if ($Version -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+-[a-z0-9.-]*rc[0-9]+$') { throw "Version must be a prerelease RC tag." }

$clientDir=(Resolve-Path -LiteralPath $ClientDir).Path
$serverDir=(Resolve-Path -LiteralPath $ServerDir).Path
$roots=@($clientDir,$serverDir)
if ($DependencyDir) { $roots+=(Resolve-Path -LiteralPath $DependencyDir).Path }
$client=Find-RequiredFile 'HKMP.dll' @($clientDir)
$server=Find-RequiredFile 'HKMPServer.exe' @($serverDir)
$serverConfig=Find-RequiredFile 'HKMPServer.exe.config' @($serverDir)
$crypto=Find-RequiredFile 'BouncyCastle.Cryptography.dll' $roots
$json=Find-RequiredFile 'Newtonsoft.Json.dll' $roots
$repositoryRoot=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$projectLicense=Find-RequiredFile 'LICENSE' @($repositoryRoot)
$thirdPartyLicenses=Join-Path $repositoryRoot 'licenses'
$cryptoLicense=Find-RequiredFile 'BouncyCastle.Cryptography-LICENSE.md' @($thirdPartyLicenses)
$jsonLicense=Find-RequiredFile 'Newtonsoft.Json-LICENSE.md' @($thirdPartyLicenses)

$serverClient=Join-Path $serverDir "HKMP.dll"
if (Test-Path -LiteralPath $serverClient -PathType Leaf) {
    $a=(Get-FileHash -LiteralPath $client -Algorithm SHA256).Hash
    $b=(Get-FileHash -LiteralPath $serverClient -Algorithm SHA256).Hash
    if ($a -ne $b) { throw "HKMP.dll differs between client and server: rebuild both from the same commit." }
}
if (Test-Path -LiteralPath $OutputDir) { throw "OutputDir already exists: $OutputDir. Use a clean destination." }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$clientFolder=Join-Path $OutputDir "HKMP-client-$Version"
$serverFolder=Join-Path $OutputDir "HKMP-server-$Version"
New-Item -ItemType Directory -Path $clientFolder | Out-Null
New-Item -ItemType Directory -Path $serverFolder | Out-Null
Copy-Item -LiteralPath $client -Destination $clientFolder
Copy-Item -LiteralPath $crypto -Destination $clientFolder
Copy-Item -LiteralPath $server -Destination $serverFolder
Copy-Item -LiteralPath $serverConfig -Destination $serverFolder
Copy-Item -LiteralPath $client -Destination $serverFolder
Copy-Item -LiteralPath $crypto -Destination $serverFolder
Copy-Item -LiteralPath $json -Destination $serverFolder
foreach ($folder in @($clientFolder,$serverFolder)) {
    Copy-Item -LiteralPath $projectLicense -Destination $folder
    $licenseFolder=Join-Path $folder 'licenses'
    New-Item -ItemType Directory -Path $licenseFolder | Out-Null
    Copy-Item -LiteralPath $cryptoLicense,$jsonLicense -Destination $licenseFolder
}

$guide=@"
HKMP co-op prerelease $Version
Source commit claimed by builder: $SourceCommit
Built against supplied Hollow Knight 1.5.78.11833 Managed assemblies.
Client: install with a matching Hollow Knight Modding API and Hook setup.
Server: run HKMPServer.exe with dependencies next to it.
Enable Full Synchronisation for enemies, world and save sync.
Original project: https://github.com/Extremelyd1/HKMP (LGPL-2.1).
Game, Unity, and proprietary DLLs are NOT included.
Gameplay has not been verified; use backed-up test saves.
Packaging does NOT prove binary provenance or gameplay correctness.
"@
Set-Content -LiteralPath (Join-Path $clientFolder 'INSTALL.txt') -Value $guide -Encoding UTF8
Set-Content -LiteralPath (Join-Path $serverFolder 'INSTALL.txt') -Value $guide -Encoding UTF8
$clientZip=Join-Path $OutputDir "HKMP-client-$Version.zip"
$serverZip=Join-Path $OutputDir "HKMP-server-$Version.zip"
Compress-Archive -Path (Join-Path $clientFolder '*') -DestinationPath $clientZip
Compress-Archive -Path (Join-Path $serverFolder '*') -DestinationPath $serverZip
$lines=@($clientZip,$serverZip) | ForEach-Object {
    $hash=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $(Split-Path -Leaf $_)"
}
Set-Content -LiteralPath (Join-Path $OutputDir 'SHA256SUMS.txt') -Value $lines -Encoding UTF8
Write-Host "Created candidate packages at: $OutputDir"

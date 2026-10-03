$ErrorActionPreference = "Stop"

param(
  [Parameter(Mandatory = $true)][string]$Target,
  [Parameter(Mandatory = $true)][string]$Out
)

$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Pkg = Join-Path $Root "PKGStream"
$Version = if ($env:PKGSTREAM_NODE_VERSION) { $env:PKGSTREAM_NODE_VERSION } else { "22.23.3" }
$Cache = Join-Path $Root ".pkgstream-build-cache"
$Work = Join-Path $Cache $Target

switch ($Target) {
  "win-x64"   { $Arch = "x64" }
  "win-x86"   { $Arch = "x86" }
  "win-arm"   { $Arch = "x86" }
  "win-arm64" { $Arch = "arm64" }
  default { throw "Unsupported PKGStream runtime target: $Target" }
}

$Archive = "node-v$Version-win-$Arch.zip"
$Url = "https://nodejs.org/dist/v$Version/$Archive"
New-Item -ItemType Directory -Force -Path $Work, (Join-Path $Out "PKGStream") | Out-Null
Remove-Item -Recurse -Force (Join-Path $Out "PKGStream") -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path (Join-Path $Out "PKGStream") | Out-Null

$ArchivePath = Join-Path $Work $Archive
if (-not (Test-Path $ArchivePath)) {
  Invoke-WebRequest -Uri $Url -OutFile $ArchivePath
}

$Extracted = Join-Path $Work "extracted"
Remove-Item -Recurse -Force $Extracted -ErrorAction SilentlyContinue
Expand-Archive -Path $ArchivePath -DestinationPath $Extracted -Force

$Node = Get-ChildItem -Path $Extracted -Filter "node.exe" -Recurse | Select-Object -First 1
if (-not $Node) { throw "Node binary not found in $Archive" }

$NodeDir = Join-Path $Out "PKGStream/node"
New-Item -ItemType Directory -Force -Path $NodeDir | Out-Null
Copy-Item $Node.FullName (Join-Path $NodeDir "node.exe")

if (-not (Test-Path (Join-Path $Pkg "node_modules/@mary/rar"))) {
  Push-Location $Pkg
  try { npm install --omit=dev }
  finally { Pop-Location }
}

Copy-Item (Join-Path $Pkg "package.json") (Join-Path $Out "PKGStream/package.json") -Force
if (Test-Path (Join-Path $Pkg ".npmrc")) { Copy-Item (Join-Path $Pkg ".npmrc") (Join-Path $Out "PKGStream/.npmrc") -Force }
Copy-Item (Join-Path $Pkg "src") (Join-Path $Out "PKGStream/src") -Recurse -Force
Copy-Item (Join-Path $Pkg "node_modules") (Join-Path $Out "PKGStream/node_modules") -Recurse -Force
Remove-Item -Recurse -Force (Join-Path $Out "PKGStream/test") -ErrorAction SilentlyContinue

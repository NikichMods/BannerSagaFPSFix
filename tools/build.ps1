# SPDX-License-Identifier: MPL-2.0
param([Parameter(Mandatory=$true)][string]$OutDirectory, [switch]$Development)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$destination=[IO.Path]::GetFullPath($OutDirectory)
if (Test-Path -LiteralPath $destination) { throw 'Output directory must not exist; preserve frozen artifacts.' }
$version=(Get-Content -LiteralPath (Join-Path $projectRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+(-[A-Za-z0-9.]+)?$') { throw 'Invalid VERSION' }
$commit=(& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Git source identity unavailable' }
$dirty=@(& git -C $projectRoot status --porcelain).Count -ne 0
if ($dirty -and -not $Development) { throw 'Commit source before freezing a candidate.' }
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Windows .NET Framework compiler not found.' }
New-Item -ItemType Directory -Path $destination | Out-Null
$generated=Join-Path $destination 'BuildInfo.cs'
$numericVersion=($version -split '-')[0]+'.0'
@"
// Generated from VERSION; not source.
using System.Reflection;
[assembly: AssemblyTitle("BannerSagaFPSFix")]
[assembly: AssemblyVersion("$numericVersion")]
[assembly: AssemblyFileVersion("$numericVersion")]
[assembly: AssemblyInformationalVersion("$version")]
namespace BannerSagaFPSFix { internal static class BuildInfo { internal const string Version="$version"; internal const string Commit="$commit"; } }
"@ | Set-Content -LiteralPath $generated -Encoding UTF8
$executable=Join-Path $destination 'BannerSagaFPSFix.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output "/out:$executable" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $projectRoot 'src\Patcher.cs') (Join-Path $projectRoot 'src\Program.cs') $generated
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Remove-Item -LiteralPath $generated
$fileInfo=[Diagnostics.FileVersionInfo]::GetVersionInfo($executable)
if ($fileInfo.ProductVersion -ne $version) { throw 'Binary version mismatch.' }
foreach ($name in @('README.md','README.ru.md','LICENSE','LICENSING.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination (Join-Path $destination $name) }
$manifest=[ordered]@{version=$version;source_commit=$commit;source_dirty=$dirty;development=[bool]$Development;executable='BannerSagaFPSFix.exe';sha256=(Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant();compiler_sha256=(Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant();runtime_acceptance='pending for installer UI; game behavior accepted in BannerSagaUI test.8'}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'manifest.json') -Encoding UTF8
$archive=Join-Path (Split-Path $destination -Parent) (('BannerSagaFPSFix-'+$version+$(if ($Development) {'-development'} else {''}))+'.zip')
if (Test-Path -LiteralPath $archive) { throw 'Archive exists; frozen artifact must not be overwritten.' }
Compress-Archive -LiteralPath (Get-ChildItem -LiteralPath $destination -File | Select-Object -ExpandProperty FullName) -DestinationPath $archive
Write-Output ($manifest | ConvertTo-Json -Compress)
Write-Output $archive

# SPDX-License-Identifier: MPL-2.0
param([string]$OriginalSwf,[string]$AcceptedPatchedSwf)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$testRoot=Join-Path $projectRoot ('local\tests\'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$info=Join-Path $testRoot 'BuildInfo.cs'
'namespace BannerSagaFPSFix { internal static class BuildInfo { internal const string Version="tests"; internal const string Commit="tests"; } }' | Set-Content -LiteralPath $info -Encoding UTF8
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$tests=Join-Path $testRoot 'Tests.exe'
& $compiler /nologo /target:exe /utf8output "/out:$tests" /main:Tests /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll (Join-Path $projectRoot 'src\Patcher.cs') (Join-Path $projectRoot 'src\Program.cs') (Join-Path $projectRoot 'tests\Tests.cs') $info
if ($LASTEXITCODE -ne 0) { throw 'Test compile failed' }
$arguments=@($testRoot)
if ($OriginalSwf -or $AcceptedPatchedSwf) {
    if (-not $OriginalSwf -or -not $AcceptedPatchedSwf) { throw 'Supply both host inputs.' }
    $arguments+=@([IO.Path]::GetFullPath($OriginalSwf),[IO.Path]::GetFullPath($AcceptedPatchedSwf))
}
& $tests @arguments
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
Write-Output $testRoot

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$outputFolder = Join-Path $repoRoot 'dist'
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) {
    $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'Requires Windows with .NET Framework 4.x.' }
$iconPath = Join-Path $repoRoot 'assets\cat.ico'
$executablePath = Join-Path $outputFolder 'SalaryClock.exe'
$compilerArgs = @('/nologo','/target:winexe','/optimize+','/platform:anycpu','/codepage:65001',
    '/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/reference:System.Web.Extensions.dll',
    ('/win32icon:' + $iconPath),('/out:' + $executablePath))
foreach ($sourceName in @('SalaryCore.cs','SalaryUI.cs','Program.cs','SelfTests.cs')) {
    $compilerArgs += Join-Path (Join-Path $repoRoot 'src') $sourceName
}
& $compilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw ('Build failed: ' + $LASTEXITCODE) }
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $outputFolder '电子猫.ico') -Force
Write-Output ('Built: ' + $executablePath)


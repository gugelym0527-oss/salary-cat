$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $repoRoot 'Build.ps1')
$outputFolder = Join-Path $repoRoot 'dist'
$executablePath = Join-Path $outputFolder 'SalaryClock.exe'
foreach ($testMode in @('--self-test','--ui-test','--restore-test','--cat-test','--settle-test','--start-test')) {
    $testArgs = @($testMode)
    if ($testMode -ne '--self-test') { $testArgs += '"' + $outputFolder + '"' }
    $testProcess = Start-Process -FilePath $executablePath -ArgumentList $testArgs -WindowStyle Hidden -Wait -PassThru
    if ($testProcess.ExitCode -ne 0) {
        $errorFile = Join-Path $outputFolder '运行错误.txt'
        if (Test-Path -LiteralPath $errorFile) { Get-Content -LiteralPath $errorFile }
        throw ('Test failed: ' + $testMode)
    }
    Write-Output ('Passed: ' + $testMode)
}


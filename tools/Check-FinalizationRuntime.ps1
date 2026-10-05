[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GodotPath,
    [Parameter(Mandatory = $true)][string]$ReportDirectory,
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [ValidateSet('smoke','battle','effects','sea','optimization','menu','ui0207','voyage0207','animation0207','ui0205','tutorial0206','hints0206','victory','construction0205','trade-glyph0204','language0202','ui0204','corrections-ui','amphora-corrections','pirates-corrections','merchant-corrections','story','refinement020','port-economy-ui')]
    [string[]]$Cases = @('smoke','battle','effects','sea','optimization','menu','ui0207','voyage0207','animation0207','ui0205','tutorial0206','hints0206','victory','construction0205','trade-glyph0204','language0202','ui0204'),
    [string]$ResumeSavePath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$taskProject = (Resolve-Path -LiteralPath $ProjectRoot).Path.TrimEnd('\','/')
$taskGodot = (Resolve-Path -LiteralPath $GodotPath).Path
$taskReports = [IO.Path]::GetFullPath($ReportDirectory).TrimEnd('\','/')
if (Test-Path -LiteralPath $taskReports) { throw 'Use a new report directory; preserve earlier results.' }
if ($taskReports -eq $taskProject -or $taskReports.StartsWith($taskProject + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Write disposable saves/evidence outside the source tree.' }
if ($ResumeSavePath -and ($Cases.Count -ne 1 -or $Cases[0] -ne 'menu')) { throw 'ResumeSavePath is only for a single menu/Continue test.' }
New-Item -ItemType Directory -Path $taskReports | Out-Null
$taskResults = [Collections.Generic.List[object]]::new()
foreach ($taskCase in $Cases) {
    $taskLog = Join-Path $taskReports ($taskCase + '.log')
    $taskError = Join-Path $taskReports ($taskCase + '.err')
    if (Test-Path -LiteralPath $taskLog) { throw "Duplicate test/evidence name: $taskCase" }
    $taskSave = Join-Path $taskReports ($taskCase + '-disposable-save.json')
    $taskArguments = @('--path', ('"'+$taskProject+'"'), '--', ('--'+$taskCase+'-test'),
        ('"--save-file='+$taskSave+'"'),
        ('"--ui-settings-file='+$taskReports+'/'+$taskCase+'-interface.txt"'),
        ('"--capture='+$taskReports+'/'+$taskCase+'.png"'))
    if ($ResumeSavePath) {
        # Continue writes only to this disposable copy, never the source fixture.
        Copy-Item -LiteralPath (Resolve-Path -LiteralPath $ResumeSavePath).Path -Destination $taskSave
        $taskArguments += '--resume-only'
    }
    # This suite verifies its own language preference across process restarts.
    if ($taskCase -ne 'language0202') { $taskArguments += '--language=en' }
    $taskStarted = Get-Date
    $taskProcess = Start-Process -FilePath $taskGodot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $taskLog -RedirectStandardError $taskError
    Write-Output "Native verification: $taskCase"
    while (-not $taskProcess.WaitForExit(10000)) {
        if (((Get-Date)-$taskStarted).TotalMinutes -gt 5) {
            Stop-Process -Id $taskProcess.Id -ErrorAction SilentlyContinue
            throw "$taskCase timed out; evidence retained."
        }
    }
    $taskOutput = [string](Get-Content -LiteralPath $taskLog -Raw)
    $taskErrors = [string](Get-Content -LiteralPath $taskError -Raw)
    $taskPassed = $taskProcess.ExitCode -eq 0 -and $taskErrors -notmatch 'ERROR|Exception|error:' -and $taskOutput -match 'PASS:'
    $taskResults.Add([pscustomobject]@{ test=$taskCase; exit_code=$taskProcess.ExitCode; passed=$taskPassed; elapsed_seconds=((Get-Date)-$taskStarted).TotalSeconds })
    [IO.File]::WriteAllText((Join-Path $taskReports 'runtime-results.json'), (ConvertTo-Json -InputObject $taskResults.ToArray()) + "`n", [Text.UTF8Encoding]::new($false))
    $taskOutput -split '\r?\n' | Where-Object { $_ -match 'PASS:|FAIL:|ERROR|Exception' } | Write-Output
    if (-not $taskPassed) { throw "$taskCase failed; inspect $taskLog and $taskError." }
}
Write-Output 'PASS: requested native runtime checks. Frame pacing is a separate serial gate.'

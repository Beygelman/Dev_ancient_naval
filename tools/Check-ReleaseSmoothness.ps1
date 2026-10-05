[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GodotPath,
    [Parameter(Mandatory = $true)][string]$ReportDirectory
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$taskGodot = (Resolve-Path -LiteralPath $GodotPath).Path
$taskReports = [IO.Path]::GetFullPath($ReportDirectory)
New-Item -ItemType Directory -Path $taskReports -Force | Out-Null
# Frame timings must use the real renderer, in serial, after the final Debug build.
$taskCases = @(
    @{Name='performance-pangaea'; Extra=@('--world-kind=Pangaea','--wide-view')},
    @{Name='performance-live-fog'; Extra=@('--live-fog')}
)
foreach ($taskCase in $taskCases) {
    $taskName = $taskCase.Name
    $taskReport = Join-Path $taskReports "$taskName.txt"
    $taskLog = Join-Path $taskReports "$taskName.log"
    $taskError = Join-Path $taskReports "$taskName.err"
    $taskArguments = @('--path', ('"'+$taskRoot+'"'), '--', '--performance-test', '--verify-smoothness',
        '--scaled-map', '--opponents=4', '--map-size=Ocean', '--hints=on', '--language=en',
        ('"--save-file='+$taskReports+'/'+$taskName+'-disposable-save.json"'),
        ('"--ui-settings-file='+$taskReports+'/'+$taskName+'-interface.txt"'),
        ('"--report='+$taskReport+'"'))
    $taskArguments += $taskCase.Extra | ForEach-Object { '"'+$_+'"' }
    $taskProcess = Start-Process -FilePath $taskGodot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $taskLog -RedirectStandardError $taskError
    $taskStarted = Get-Date
    Write-Output "Checking real frame pacing: $taskName"
    while (-not $taskProcess.WaitForExit(10000)) {
        if (((Get-Date)-$taskStarted).TotalMinutes -gt 5) {
            Get-CimInstance Win32_Process -Filter "ParentProcessId=$($taskProcess.Id)" |
                ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue }
            Stop-Process -Id $taskProcess.Id -ErrorAction SilentlyContinue
            throw "$taskName timed out. Do not package this build."
        }
    }
    $taskProcess.Refresh()
    $taskErrors = Get-Content -LiteralPath $taskError -Raw
    if ($taskProcess.ExitCode -ne 0 -or $taskErrors -match 'ERROR|Exception|error:' -or
        -not (Test-Path -LiteralPath $taskReport) -or
        -not ((Get-Content -LiteralPath $taskLog -Raw) -match 'PASS: real-map interaction profiling completed')) {
        throw "$taskName failed. Inspect $taskLog and $taskError before packaging."
    }
    Get-Content -LiteralPath $taskReport -TotalCount 2
}
Write-Output 'PASS: native frame-pacing gates. Keep these reports with the release.'

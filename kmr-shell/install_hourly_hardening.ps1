<#
.SYNOPSIS
  Registers (or removes) the "K-Meleon-R Hourly Hardening" scheduled task that runs the bundled
  GEM-OS hardening engine silently every hour.

.DESCRIPTION
  The task runs Hardening_Lockdown_Standalone.bat with the highest privileges so no UAC prompt is
  shown, hidden (no console window), and repeats every hour indefinitely plus once at start-up.
  A newline is piped into the batch so its trailing `pause` cannot block the task.

  Requires administrator rights (the batch itself hardens machine-wide Windows policy). Run without
  elevation and it exits with a clear message instead of registering a half-working task.

.PARAMETER ScriptPath
  Full path to Hardening_Lockdown_Standalone.bat. Defaults to the copy next to this script.

.PARAMETER Remove
  Unregister the task instead of installing it.
#>
[CmdletBinding()]
param(
    [string]$ScriptPath = (Join-Path $PSScriptRoot 'Hardening_Lockdown_Standalone.bat'),
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'
$TaskName = 'K-Meleon-R Hourly Hardening'
$TaskPath = '\K-Meleon-R\'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

if ($Remove) {
    try {
        Unregister-ScheduledTask -TaskName $TaskName -TaskPath $TaskPath -Confirm:$false -ErrorAction Stop
        Write-Host "Removed scheduled task '$TaskName'."
    } catch {
        Write-Host "Scheduled task '$TaskName' was not present."
    }
    return
}

if (-not (Test-Admin)) {
    Write-Warning 'Administrator rights are required to register the hourly hardening task. Skipped.'
    exit 1
}

if (-not (Test-Path -LiteralPath $ScriptPath)) {
    throw "Hardening batch not found: $ScriptPath"
}
$ScriptPath = (Resolve-Path -LiteralPath $ScriptPath).Path
$workDir = Split-Path -Parent $ScriptPath

# Run hidden through cmd.exe; `echo.` feeds a newline so the batch's trailing `pause` returns at once.
$cmd = Join-Path $env:SystemRoot 'System32\cmd.exe'
$arg = '/d /c "echo. | ""{0}"""' -f $ScriptPath
$action = New-ScheduledTaskAction -Execute $cmd -Argument $arg -WorkingDirectory $workDir

# Fire once now and repeat every hour forever, and again after every start-up.
$hourly = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Hours 1)
$atBoot = New-ScheduledTaskTrigger -AtStartup

# S4U + Highest = the batch runs elevated (High integrity) with no UAC prompt, whether or not a
# user is logged on, and without storing a password.
$user = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType S4U -RunLevel Highest

$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
    -StartWhenAvailable -Hidden -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 30) -RestartCount 2 -RestartInterval (New-TimeSpan -Minutes 5)

Register-ScheduledTask -TaskName $TaskName -TaskPath $TaskPath -Action $action `
    -Trigger @($hourly, $atBoot) -Principal $principal -Settings $settings -Force `
    -Description 'Runs the bundled GEM-OS hardening engine (Hardening_Lockdown_Standalone.bat) silently every hour. Installed by K-Meleon-R.' | Out-Null

Write-Host "Registered scheduled task '$TaskName' (hourly, hidden, highest privileges)."

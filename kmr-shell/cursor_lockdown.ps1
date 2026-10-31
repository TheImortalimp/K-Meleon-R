#requires -Version 5.1
# Installer-time cursor lockdown (run elevated). Applies to every local profile and the Default profile:
#  - blocks themes/Settings from changing pointers
#  - resets any pointer slot that is not empty or a real file under %WINDIR%\Cursors (e.g. GEM-style themed .cur sets)
$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$log = Join-Path $env:ProgramData 'AI-WARP-CURSOR-LOCKDOWN.log'
Start-Transcript -Path $log -Append | Out-Null

$slots = 'Arrow','Help','AppStarting','Wait','Crosshair','IBeam','NWPen','No','SizeNS','SizeWE','SizeNWSE','SizeNESW','SizeAll','UpArrow','Hand','Pin','Person'
$cursorRoot = Join-Path $env:SystemRoot 'Cursors'

function Test-ApprovedCursor([string]$raw) {
    if ([string]::IsNullOrWhiteSpace($raw)) { return $true }
    $p = [Environment]::ExpandEnvironmentVariables($raw.Trim().Trim('"'))
    if ($p.StartsWith('\\') -or $p.IndexOf(':', 2) -ge 0 -or ($p -split '[\\/]') -contains '..') { return $false }
    try { $full = [IO.Path]::GetFullPath($p) } catch { return $false }
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { return $false }
    $item = Get-Item -LiteralPath $full -Force
    if (($item.LinkType -in 'SymbolicLink', 'Junction') -or ($item.Extension -notin '.cur', '.ani')) { return $false }
    # Resolve DOS 8.3 short names before comparing against the system cursor directory.
    $fso = New-Object -ComObject Scripting.FileSystemObject
    $long = (Get-Item -LiteralPath $fso.GetFile($full).Path -Force).FullName
    return $long.StartsWith($cursorRoot + '\', [StringComparison]::OrdinalIgnoreCase)
}

try {
    $principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Not elevated.' }

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backupDir = Join-Path $env:ProgramData "AI-WARP-cursor-backup_$stamp"
    New-Item -ItemType Directory -Path $backupDir | Out-Null

    # Machine-wide policy
    & reg.exe add 'HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization' /v NoChangingMousePointers /t REG_DWORD /d 1 /f | Out-Null

    $targets = @()
    foreach ($k in Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList') {
        if ($k.PSChildName -notmatch '^S-1-5-21-') { continue }
        $path = (Get-ItemProperty $k.PSPath).ProfileImagePath
        if ($path -and (Test-Path (Join-Path $path 'NTUSER.DAT'))) {
            $targets += [pscustomobject]@{ Name = $k.PSChildName; Sid = $k.PSChildName; Dat = (Join-Path $path 'NTUSER.DAT') }
        }
    }
    $targets += [pscustomobject]@{ Name = 'DefaultProfile'; Sid = $null; Dat = 'C:\Users\Default\NTUSER.DAT' }

    foreach ($t in $targets) {
        $loadedHere = $false
        if ($t.Sid -and (Test-Path "Registry::HKEY_USERS\$($t.Sid)")) { $root = "HKU\$($t.Sid)" }
        else {
            $root = "HKU\AIWARPLOCK_$($t.Name)"
            & reg.exe load $root $t.Dat | Out-Null
            if ($LASTEXITCODE -ne 0) { Write-Output "SKIP $($t.Name): could not load hive"; continue }
            $loadedHere = $true
        }
        try {
            $themes = "$root\Software\Microsoft\Windows\CurrentVersion\Themes"
            $policy = "$root\Software\Policies\Microsoft\Windows\Personalization"
            $cur = "$root\Control Panel\Cursors"
            foreach ($pair in @(@($themes, 'Themes'), @($policy, 'Personalization'), @($cur, 'Cursors'))) {
                & cmd.exe /c "reg.exe export `"$($pair[0])`" `"$(Join-Path $backupDir "$($t.Name)_$($pair[1]).reg")`" /y >nul 2>&1"
            }
            & reg.exe add $themes /v ThemeChangesMousePointers /t REG_DWORD /d 0 /f | Out-Null
            & reg.exe add $policy /v NoChangingMousePointers /t REG_DWORD /d 1 /f | Out-Null

            $psCur = "Registry::" + ($cur -replace '^HKU', 'HKEY_USERS')
            $reset = 0
            if (Test-Path -LiteralPath $psCur) {
                $props = Get-ItemProperty -LiteralPath $psCur
                foreach ($s in $slots) {
                    $v = $props.$s
                    if (-not (Test-ApprovedCursor $v)) {
                        & reg.exe add $cur /v $s /t REG_EXPAND_SZ /d '' /f | Out-Null
                        Write-Output "RESET $($t.Name) ${s}: $v"
                        $reset++
                    }
                }
            }
            Write-Output "OK $($t.Name): policy locked, $reset slot(s) reset"
        } finally {
            if ($loadedHere) {
                [gc]::Collect(); [gc]::WaitForPendingFinalizers()
                & reg.exe unload $root | Out-Null
            }
        }
    }
    Write-Output "Backups: $backupDir"
    Write-Output 'DONE'
} catch {
    Write-Output "FAILED: $($_.Exception.Message)"
    exit 1
} finally {
    Stop-Transcript | Out-Null
}

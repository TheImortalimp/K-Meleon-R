<#
.SYNOPSIS
  Detects and hardens against the GEM OS style theme/cursor takeover.

.DESCRIPTION
  Default mode is read-only (Scan). Nothing is deleted at any point.
    -Quarantine  move suspicious theme/cursor files found in user-writable folders to a quarantine folder
                 (hashes and original paths are logged so every move can be reversed by hand)
    -Apply       (needs elevation) block theme, sound-scheme and pointer changes for every local profile and
                 the Default profile, and run cursor_lockdown.ps1 if it sits next to this script
  Known-bad indicators are the SHA-256 hashes of the Archive.org gem_os package from the static review.
  A clean scan does not prove a machine is safe; it only checks the theme/cursor takeover route.
#>
[CmdletBinding()]
param(
    [switch]$Quarantine,
    [switch]$Apply,
    [string[]]$ExtraScanPath = @()
)

$ErrorActionPreference = 'Continue'
$Win = $env:WINDIR
$SysCursors = (Join-Path $Win 'Cursors')
$Stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$Log = Join-Path $env:ProgramData 'GEM-OS-HARDENING.log'
$findings = New-Object System.Collections.Generic.List[object]

$KnownBad = @{
    '3e459ccdc8d78e4ce246dfe7ad2abadd515692a8c1350a37ea40b1936bb45355' = 'gem_os.zip (outer package)'
    '4ba80e72fcc437836feb89effae996735493e7bc9af5e18accfe7bdc45712ce6' = 'GEM.zip (inner package)'
}
$ThemeExt = '.theme', '.themepack', '.deskthemepack'
$CursorExt = '.cur', '.ani'
$CursorKeys = 'Arrow','Help','AppStarting','Wait','Crosshair','IBeam','NWPen','No','SizeNS','SizeWE','SizeNWSE','SizeNESW','SizeAll','UpArrow','Hand','Pin','Person'

function Write-Log([string]$m) {
    $line = '{0} {1}' -f (Get-Date -Format s), $m
    try { Add-Content -Path $Log -Value $line -ErrorAction Stop } catch { }
}
function Add-Finding([string]$sev, [string]$what, [string]$where) {
    $findings.Add([pscustomobject]@{ Severity = $sev; Finding = $what; Location = $where })
    Write-Log "[$sev] $what :: $where"
}

# A cursor path is trusted only if it is empty, or a real .cur/.ani under %WINDIR%\Cursors with no links or odd syntax.
function Test-SystemCursorPath([string]$raw) {
    if ([string]::IsNullOrWhiteSpace($raw)) { return $true }
    $p = [Environment]::ExpandEnvironmentVariables($raw.Trim().Trim('"'))
    if ($p -match '^(\\\\|//)' -or $p -match ':.*:' -or $p -match '\.\.') { return $false }
    try { $full = [IO.Path]::GetFullPath($p) } catch { return $false }
    if (-not $full.StartsWith($SysCursors + '\', [StringComparison]::OrdinalIgnoreCase)) { return $false }
    $item = Get-Item -LiteralPath $full -Force -ErrorAction SilentlyContinue
    if (-not $item) { return $false }
    if (($item.LinkType -in 'SymbolicLink', 'Junction') -or ($item.Extension -notin $CursorExt)) { return $false }
    return $true
}

Write-Host "== GEM OS hardening ($(if ($Apply) {'APPLY'} elseif ($Quarantine) {'QUARANTINE'} else {'SCAN, read-only'})) =="

# 1. Active pointer slots for the current user
$cur = Get-ItemProperty 'HKCU:\Control Panel\Cursors' -ErrorAction SilentlyContinue
foreach ($k in $CursorKeys) {
    $v = $cur.$k
    if ($v -and -not (Test-SystemCursorPath $v)) { Add-Finding 'HIGH' "Non-system pointer in slot '$k'" $v }
}

# 2. Saved pointer schemes that point outside the system folder
$schemes = Get-ItemProperty 'HKCU:\Control Panel\Cursors\Schemes' -ErrorAction SilentlyContinue
if ($schemes) {
    foreach ($prop in $schemes.PSObject.Properties) {
        if ($prop.Name -like 'PS*') { continue }
        foreach ($part in ([string]$prop.Value).Split(',')) {
            if ($part -and -not (Test-SystemCursorPath $part)) { Add-Finding 'MEDIUM' "Saved pointer scheme '$($prop.Name)' uses a non-system file" $part; break }
        }
    }
}

# 3. Current theme file
$ct = (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes' -Name CurrentTheme -ErrorAction SilentlyContinue).CurrentTheme
$stdCustom = Join-Path $env:LOCALAPPDATA 'Microsoft\Windows\Themes'
if ($ct -and -not $ct.StartsWith((Join-Path $Win 'Resources'), [StringComparison]::OrdinalIgnoreCase) -and -not $ct.StartsWith($stdCustom, [StringComparison]::OrdinalIgnoreCase)) {
    Add-Finding 'LOW' 'Current theme file is outside %WINDIR%\Resources' $ct
}

# 4. Files in user-writable places
$roots = @()
foreach ($u in (Get-ChildItem 'C:\Users' -Directory -ErrorAction SilentlyContinue)) {
    foreach ($sub in 'Downloads', 'Desktop', 'Documents', 'AppData\Local\Temp') {
        $d = Join-Path $u.FullName $sub; if (Test-Path -LiteralPath $d -ErrorAction SilentlyContinue) { $roots += $d }
    }
}
$roots += $ExtraScanPath | Where-Object { $_ -and (Test-Path -LiteralPath $_ -ErrorAction SilentlyContinue) }
$suspect = New-Object System.Collections.Generic.List[string]
$seen = @{}
foreach ($r in ($roots | Select-Object -Unique)) {
    Get-ChildItem -LiteralPath $r -Recurse -File -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in ($ThemeExt + $CursorExt + '.zip') -and $_.Length -lt 200MB } |
        Where-Object { $k = $_.FullName.ToLower(); if ($seen.ContainsKey($k)) { $false } else { $seen[$k] = $true; $true } } |
        ForEach-Object {
            $f = $_
            $hash = (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256 -ErrorAction SilentlyContinue).Hash
            if ($hash -and $KnownBad.ContainsKey($hash.ToLower())) {
                Add-Finding 'CRITICAL' ("Known GEM OS package: " + $KnownBad[$hash.ToLower()]) $f.FullName
                $suspect.Add($f.FullName)
            }
            elseif ($f.Extension -in $ThemeExt) {
                Add-Finding 'MEDIUM' 'Theme file in a user-writable folder' $f.FullName
                $suspect.Add($f.FullName)
                if ($f.Extension -eq '.theme') {
                    $bad = Select-String -LiteralPath $f.FullName -Pattern '^\s*(Arrow|Help|AppStarting|Wait|Crosshair|IBeam|NWPen|No|SizeNS|SizeWE|SizeNWSE|SizeNESW|SizeAll|UpArrow|Hand|DefaultValue)\s*=\s*(.+)$' -ErrorAction SilentlyContinue |
                        Where-Object { $_.Matches[0].Groups[2].Value -match '\.(cur|ani)\s*$' -and -not $_.Matches[0].Groups[2].Value.ToLower().Contains('%systemroot%\cursors') -and -not $_.Matches[0].Groups[2].Value.ToLower().Contains('%windir%\cursors') }
                    if ($bad) { Add-Finding 'HIGH' "Theme remaps $(@($bad).Count) pointer(s) to non-system files" $f.FullName }
                }
            }
            elseif ($f.Extension -in $CursorExt) {
                Add-Finding 'LOW' 'Loose cursor file in a user-writable folder' $f.FullName
                $suspect.Add($f.FullName)
            }
        }
}

# 5. Policy state
$pol = Get-ItemProperty 'HKLM:\Software\Policies\Microsoft\Windows\Personalization' -ErrorAction SilentlyContinue
if (-not $pol -or $pol.NoChangingMousePointers -ne 1) { Add-Finding 'INFO' 'Pointer-change policy is not set machine-wide (run with -Apply)' 'HKLM Personalization policy' }

# Quarantine: move, never delete
if ($Quarantine -and $suspect.Count -gt 0) {
    $qdir = Join-Path $env:ProgramData "GEM-OS-QUARANTINE\$Stamp"
    New-Item -ItemType Directory -Path $qdir -Force | Out-Null
    $map = @()
    foreach ($path in ($suspect | Select-Object -Unique)) {
        $h = (Get-FileHash -LiteralPath $path -Algorithm SHA256 -ErrorAction SilentlyContinue).Hash
        $dest = Join-Path $qdir ('{0}_{1}.quarantined' -f $h.Substring(0, 12), (Split-Path $path -Leaf))
        try {
            Move-Item -LiteralPath $path -Destination $dest -Force -ErrorAction Stop
            $map += [pscustomobject]@{ Original = $path; Quarantined = $dest; SHA256 = $h }
            Write-Log "QUARANTINED $path -> $dest"
        } catch { Write-Log "QUARANTINE FAILED $path : $($_.Exception.Message)" }
    }
    $map | ConvertTo-Json | Set-Content -Path (Join-Path $qdir 'manifest.json')
    Write-Host "Quarantined $($map.Count) file(s) to $qdir (manifest.json lists original paths)"
}

# Apply: block theme/sound/pointer changes for all profiles
if ($Apply) {
    $admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $admin) { Write-Warning 'Run elevated to use -Apply.'; exit 2 }

    $backup = Join-Path $env:ProgramData "GEM-OS-HARDENING-BACKUP\$Stamp"
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    $machine = @(
        @{ P = 'HKLM:\Software\Policies\Microsoft\Windows\Personalization'; N = 'NoChangingMousePointers'; V = 1 },
        @{ P = 'HKLM:\Software\Policies\Microsoft\Windows\Personalization'; N = 'NoChangingSoundScheme'; V = 1 }
    )
    $perUser = @(
        @{ P = 'Software\Microsoft\Windows\CurrentVersion\Themes'; N = 'ThemeChangesMousePointers'; V = 0 },
        @{ P = 'Software\Policies\Microsoft\Windows\Personalization'; N = 'NoChangingMousePointers'; V = 1 },
        @{ P = 'Software\Policies\Microsoft\Windows\Personalization'; N = 'NoChangingSoundScheme'; V = 1 },
        @{ P = 'Software\Microsoft\Windows\CurrentVersion\Policies\Explorer'; N = 'NoThemesTab'; V = 1 }
    )
    function Set-Backed([string]$path, [string]$name, [int]$value) {
        $old = (Get-ItemProperty -Path $path -Name $name -ErrorAction SilentlyContinue).$name
        Add-Content -Path (Join-Path $backup 'previous-values.txt') -Value ("{0}\{1} = {2}" -f $path, $name, $(if ($null -eq $old) { '<absent>' } else { $old }))
        if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
        New-ItemProperty -Path $path -Name $name -Value $value -PropertyType DWord -Force | Out-Null
        Write-Log "SET $path\$name = $value"
    }
    foreach ($m in $machine) { Set-Backed $m.P $m.N $m.V }

    $profiles = Get-ChildItem 'HKLM:\Software\Microsoft\Windows NT\CurrentVersion\ProfileList' -ErrorAction SilentlyContinue |
        Where-Object { $_.PSChildName -match '^S-1-5-21-' }
    foreach ($pr in $profiles) {
        $sid = $pr.PSChildName
        $loaded = Test-Path "Registry::HKEY_USERS\$sid"
        $dat = Join-Path ([Environment]::ExpandEnvironmentVariables((Get-ItemProperty $pr.PSPath).ProfileImagePath)) 'NTUSER.DAT'
        $hive = "HKU\GEMTMP_$($sid.Split('-')[-1])"
        if (-not $loaded) { & reg.exe load $hive $dat 2>&1 | Out-Null }
        $root = if ($loaded) { "Registry::HKEY_USERS\$sid" } else { "Registry::$hive" }
        foreach ($u in $perUser) { Set-Backed "$root\$($u.P)" $u.N $u.V }
        if (-not $loaded) { [GC]::Collect(); Start-Sleep -Milliseconds 300; & reg.exe unload $hive 2>&1 | Out-Null }
    }

    $ps1 = Join-Path $PSScriptRoot 'cursor_lockdown.ps1'
    if (Test-Path $ps1) { & $ps1 }
    Write-Host "Applied. Previous values saved under $backup"
}

if ($findings.Count -eq 0) { Write-Host 'No findings.' -ForegroundColor Green }
else { $findings | Sort-Object { switch ($_.Severity) { 'CRITICAL' {0} 'HIGH' {1} 'MEDIUM' {2} 'LOW' {3} default {4} } } | Format-Table -AutoSize -Wrap }
Write-Host "Log: $Log"

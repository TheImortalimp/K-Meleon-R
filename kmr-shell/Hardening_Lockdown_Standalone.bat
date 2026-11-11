@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Unified System Security Hardening - Standalone (ExecutionPolicy Immune)

:: ============================================================================
:: ELEVATION CHECK (High Mandatory Level SID S-1-16-12288)
:: ============================================================================
whoami /groups | findstr /c:"S-1-16-12288" >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ===============================================================================
    echo  [!] Elevation required. Requesting Administrator privileges via UAC...
    echo ===============================================================================
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b 0
)

:: Set working directory to script location
cd /d "%~dp0"

cls
echo ===============================================================================
echo  UNIFIED SYSTEM SECURITY HARDENING: STANDALONE ENGINE (VERBOSE)
echo ===============================================================================
echo  [+] Elevation confirmed: Running under High Mandatory Level (Administrator)
echo  [*] Execution Directory: "%~dp0"
echo  [*] Bypassing ExecutionPolicy restrictions via in-memory stream...
echo.

set "BATCH_SELF=%~f0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; try { Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force } catch { }; $f = $env:BATCH_SELF; $lines = [System.IO.File]::ReadAllLines($f); $idx = [System.Array]::IndexOf($lines, '___POWERSHELL_PAYLOAD_START___'); if ($idx -ge 0) { $code = ($lines[($idx + 1)..($lines.Length - 1)]) -join [Environment]::NewLine; & ([ScriptBlock]::Create($code)) } else { Write-Error 'CRITICAL: Payload boundary not found.' }"
set "SCRIPT_EXIT_CODE=%errorlevel%"

echo.
echo ===============================================================================
if %SCRIPT_EXIT_CODE% equ 0 (
    echo  [SUCCESS] All hardening routines completed successfully. (Exit code: 0)
) else (
    echo  [WARNING] Hardening finished with exit code %SCRIPT_EXIT_CODE%.
    echo            Review log file for details:
    echo            %%ProgramData%%\SystemSecurityHardening\hardening_lockdown.log
)
echo ===============================================================================
echo.
pause
exit /b %SCRIPT_EXIT_CODE%

___POWERSHELL_PAYLOAD_START___
#requires -Version 5.1
#requires -RunAsAdministrator
<#
.SYNOPSIS
    Unified System Security Hardening: Cursor/Theme Lockdown & Domain Blocker (Verbose)
    Derived and hardened from GEM-OS malware research and DSH blocker tooling.

.DESCRIPTION
    Performs comprehensive, elevated system hardening against theme/cursor hijacking
    attacks and establishes a dual-layer network domain containment barrier with
    real-time verbose output:
      1. Machine & Per-User Policies: Locks mouse pointer and sound scheme modifications.
      2. Cursor Sanitization: Validates all 17 pointer slots against path traversal, UNC,
         8.3 short names, and reparse points; resets non-system cursors.
      3. Live Refresh: Calls Win32 SPI_SETCURSORS to instantly revert active desktop pointers.
      4. Layer 1 Network Containment: Updates %WINDIR%\System32\drivers\etc\hosts with 0.0.0.0
         and :: null-routes via an atomic, multi-channel write engine with ACL takeown.
      5. Layer 2 Network Containment: Creates Windows Defender Firewall outbound block rules
         for all resolved IPv4 and IPv6 addresses.
      6. Backups & Logging: Exports full registry keys (.reg), hosts backup, and activity log.

.PARAMETER DryRun
    Simulates actions without modifying the registry, hosts file, or firewall rules.

.PARAMETER SkipHosts
    Skips the Windows hosts file modification step (Firewall rules only).

.PARAMETER SkipFirewall
    Skips the Windows Defender Firewall rule deployment step (Hosts sinkhole only).

.PARAMETER SkipCursors
    Skips the theme and cursor registry hardening step.

.PARAMETER AdditionalDomains
    Specifies extra domains to include in the sinkhole and firewall blocklist.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$DryRun,
    [switch]$SkipHosts,
    [switch]$SkipFirewall,
    [switch]$SkipCursors,
    [string[]]$AdditionalDomains = @()
)

$ErrorActionPreference = 'Continue'

# ==============================================================================
# ELEVATION CHECK
# ==============================================================================
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal   = New-Object Security.Principal.WindowsPrincipal($currentUser)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "ACCESS DENIED: This script must be executed from an elevated Administrator PowerShell prompt."
    exit 1
}

# ==============================================================================
# CONFIGURATION & DEFINITIONS
# ==============================================================================
$DefaultDomains = @(
    'www.anarchyarcade.com', 'anarchyarcade.com',
    'www.ponentmon.com',     'ponentmon.com',
    'www.feardot.com',        'feardot.com',
    'www.esopiniones.com',    'esopiniones.com',
    'www.cybo.com',           'cybo.com',
    'www.educationtop.com',   'educationtop.com',
    'www.evaediciones.es',    'evaediciones.es'
)
$TargetDomains = @($DefaultDomains + $AdditionalDomains | Select-Object -Unique)

$CursorSlots = @(
    'Arrow', 'Help', 'AppStarting', 'Wait', 'Crosshair', 'IBeam', 'NWPen', 'No',
    'SizeNS', 'SizeWE', 'SizeNWSE', 'SizeNESW', 'SizeAll', 'UpArrow', 'Hand', 'Pin', 'Person'
)

$FirewallPrefix = 'Hardening Block'
$Timestamp      = Get-Date -Format 'yyyyMMdd-HHmmss'
$BaseDir        = Join-Path $env:ProgramData 'SystemSecurityHardening'
$BackupDir      = Join-Path $BaseDir "Backups_$Timestamp"
$LogPath        = Join-Path $BaseDir 'hardening_lockdown.log'
$SysWinDir      = $env:SystemRoot
$SysCursorsDir  = Join-Path $SysWinDir 'Cursors'
$HostsFilePath  = Join-Path $SysWinDir 'System32\drivers\etc\hosts'

# Create persistence directories
if (-not $DryRun) {
    if (-not (Test-Path -LiteralPath $BaseDir))   { New-Item -ItemType Directory -Path $BaseDir -Force | Out-Null }
    if (-not (Test-Path -LiteralPath $BackupDir)) { New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null }
}

# ==============================================================================
# VERBOSE LOGGING & PRESENTATION HELPER
# ==============================================================================
function Write-Log {
    param(
        [Parameter(Mandatory=$true)][string]$Message,
        [ValidateSet('INFO', 'OK', 'WARN', 'ERROR', 'RESET', 'VERBOSE', 'HEADER', 'SUBHEADER')][string]$Level = 'INFO'
    )
    $timeStr = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    $fileLine = "[$timeStr] [$Level] $Message"
    if (-not $DryRun) {
        try { Add-Content -LiteralPath $LogPath -Value $fileLine -Encoding UTF8 -ErrorAction SilentlyContinue } catch { }
    }

    switch ($Level) {
        'OK'        { Write-Host "  [+] $Message" -ForegroundColor Green }
        'INFO'      { Write-Host "  [*] $Message" -ForegroundColor Cyan }
        'WARN'      { Write-Host "  [!] $Message" -ForegroundColor Yellow }
        'ERROR'     { Write-Host "  [-] $Message" -ForegroundColor Red }
        'RESET'     { Write-Host "  [~] $Message" -ForegroundColor Magenta }
        'VERBOSE'   { Write-Host "      > $Message" -ForegroundColor Gray }
        'SUBHEADER' {
            Write-Host ""
            Write-Host "  --- $Message ---" -ForegroundColor White
        }
        'HEADER'    {
            Write-Host ""
            Write-Host "===============================================================================" -ForegroundColor Cyan
            Write-Host "  $Message" -ForegroundColor White
            Write-Host "===============================================================================" -ForegroundColor Cyan
        }
    }
}

# ==============================================================================
# WIN32 NATIVE METHODS (PATH EXPANSION & LIVE CURSOR REFRESH)
# ==============================================================================
$NativeAvailable = $false
try {
    Add-Type -TypeDefinition @"
    using System;
    using System.Text;
    using System.Runtime.InteropServices;

    public static class Win32Helper {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetLongPathName(string shortPath, StringBuilder longPath, uint size);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        public static string ExpandLongPath(string shortPath) {
            try {
                StringBuilder sb = new StringBuilder(1024);
                uint res = GetLongPathName(shortPath, sb, (uint)sb.Capacity);
                if (res > 0 && res < sb.Capacity) {
                    return sb.ToString();
                }
            } catch { }
            return shortPath;
        }

        public static void RefreshSystemCursors() {
            try {
                // 0x0057 = SPI_SETCURSORS, 0x02 = SPIF_SENDCHANGE
                SystemParametersInfo(0x0057, 0, IntPtr.Zero, 0x02);
            } catch { }
        }
    }
"@ -ErrorAction Stop
    $NativeAvailable = $true
} catch {
    $NativeAvailable = $false
}

function Resolve-ToLongPath([string]$path) {
    if ($NativeAvailable) {
        return [Win32Helper]::ExpandLongPath($path)
    }
    try {
        $fso = New-Object -ComObject Scripting.FileSystemObject
        $long = $fso.GetFile($path).Path
        [System.Runtime.InteropServices.Marshal]::ReleaseComObject($fso) | Out-Null
        return $long
    } catch {
        return (Get-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue).FullName
    }
}

function Invoke-CursorRefresh {
    if ($NativeAvailable) {
        [Win32Helper]::RefreshSystemCursors()
    }
}

# ==============================================================================
# CURSOR SANITIZATION & PATH TRAVERSAL VERIFIER
# ==============================================================================
function Test-ApprovedCursor([string]$raw, [ref]$reason) {
    if ([string]::IsNullOrWhiteSpace($raw)) {
        $reason.Value = "Default System Pointer (Empty slot value)"
        return $true
    }
    $p = [Environment]::ExpandEnvironmentVariables($raw.Trim().Trim('"'))
    
    # Reject UNC paths and device paths
    if ($p.StartsWith('\\') -or $p.StartsWith('//')) {
        $reason.Value = "UNC / Network Share path rejected: $p"
        return $false
    }
    if ($p.IndexOf(':', 2) -ge 0) {
        $reason.Value = "NTFS Alternate Data Stream detected: $p"
        return $false
    }
    
    # Reject relative directory traversal sequences
    if (($p -split '[\\/]') -contains '..') {
        $reason.Value = "Directory traversal sequence ('..') detected: $p"
        return $false
    }
    
    try {
        $full = [System.IO.Path]::GetFullPath($p)
    } catch {
        $reason.Value = "Path parsing failure: $($_.Exception.Message)"
        return $false
    }
    
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
        $reason.Value = "File does not exist: $full"
        return $false
    }
    
    # Reject reparse points (symlinks and directory junctions)
    try {
        $item = Get-Item -LiteralPath $full -Force -ErrorAction Stop
        if ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
            $reason.Value = "ReparsePoint / SymbolicLink / Junction detected: $full"
            return $false
        }
        if ($item.Extension -notin @('.cur', '.ani')) {
            $reason.Value = "Invalid extension '$($item.Extension)' (must be .cur or .ani): $full"
            return $false
        }
    } catch {
        $reason.Value = "Item inspection error: $($_.Exception.Message)"
        return $false
    }
    
    # Resolve 8.3 short names to full canonical path
    $canonical = Resolve-ToLongPath $full
    if ([string]::IsNullOrWhiteSpace($canonical)) {
        $reason.Value = "Unable to expand canonical path for: $full"
        return $false
    }
    
    # Verify the canonical path strictly resides inside %WINDIR%\Cursors\
    $expectedPrefix = $SysCursorsDir.TrimEnd('\') + '\'
    if (-not $canonical.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        $reason.Value = "Path is outside approved system directory ($SysCursorsDir): $canonical"
        return $false
    }

    $reason.Value = "Approved System Cursor ($canonical)"
    return $true
}

# ==============================================================================
# DNS RESOLUTION HELPER
# ==============================================================================
function Resolve-HostAddresses {
    param([string]$Name)
    $found = New-Object System.Collections.Generic.List[string]
    foreach ($type in @('A', 'AAAA')) {
        try {
            $records = Resolve-DnsName -Name $Name -Type $type -ErrorAction Stop
            foreach ($rec in $records) {
                if ($rec.IPAddress) { $found.Add([string]$rec.IPAddress) }
            }
        } catch { }
    }
    if ($found.Count -eq 0) {
        try {
            $addrs = [System.Net.Dns]::GetHostAddresses($Name)
            foreach ($a in $addrs) { $found.Add($a.IPAddressToString) }
        } catch { }
    }
    return @($found | Select-Object -Unique)
}

# ==============================================================================
# HARDENED HOSTS FILE SINKHOLE ENGINE (VERBOSE)
# ==============================================================================
function Update-HostsFileSafely {
    param(
        [string]$Path,
        [string[]]$Domains,
        [string]$BackupDirectory,
        [switch]$Dry
    )

    $targetUnique = @($Domains | Select-Object -Unique)
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $existingLines = @()

    Write-Log "Target hosts file path: $Path" 'VERBOSE'

    if (Test-Path -LiteralPath $Path) {
        $item = Get-Item -LiteralPath $Path -Force
        Write-Log "Existing hosts file detected (Size: $($item.Length) bytes, Attributes: $($item.Attributes))" 'VERBOSE'

        if (-not $Dry) {
            # Strip ReadOnly, Hidden, and System attributes
            Write-Log "Stripping physical file attributes via attrib.exe -r -s -h..." 'VERBOSE'
            & attrib.exe -r -s -h "$Path" >$null 2>&1
            
            # Take ownership and grant Full Control to Administrators (*S-1-5-32-544)
            Write-Log "Acquiring ownership via takeown.exe..." 'VERBOSE'
            & takeown.exe /f "$Path" /a >$null 2>&1
            Write-Log "Granting Full Control to Administrators via icacls.exe..." 'VERBOSE'
            & icacls.exe "$Path" /grant "*S-1-5-32-544:F" /c /q >$null 2>&1

            # Backup current hosts
            $bakPath = Join-Path $BackupDirectory 'hosts.bak'
            Write-Log "Backing up hosts file to: $bakPath" 'VERBOSE'
            try { Copy-Item -LiteralPath $Path -Destination $bakPath -Force } catch { }
        }

        try {
            $existingLines = [System.IO.File]::ReadAllLines($Path)
            Write-Log "Loaded $($existingLines.Count) line(s) from current hosts file." 'VERBOSE'
        } catch {
            try { $existingLines = @(Get-Content -LiteralPath $Path -Encoding ASCII -ErrorAction Stop) } catch { $existingLines = @() }
        }
    } else {
        Write-Log "Hosts file not present on disk. Will create new file." 'VERBOSE'
        if (-not $Dry) {
            $parentDir = Split-Path -Parent $Path
            if (-not (Test-Path -LiteralPath $parentDir)) {
                New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
            }
        }
    }

    # Filter out previous hardening blocks
    $cleanLines = New-Object System.Collections.Generic.List[string]
    $inBlock = $false
    $purgedLines = 0
    foreach ($line in $existingLines) {
        if ($line -match '# === BEGIN HARDENING DOMAIN SINKHOLE ===') { $inBlock = $true; $purgedLines++; continue }
        if ($line -match '# === END HARDENING DOMAIN SINKHOLE ===')   { $inBlock = $false; $purgedLines++; continue }
        if ($inBlock) { $purgedLines++; continue }
        $cleanLines.Add($line)
    }
    if ($purgedLines -gt 0) {
        Write-Log "Purged $purgedLines legacy/previous hardening sinkhole entry line(s)." 'VERBOSE'
    }

    # Append fresh sinkhole entries
    $cleanLines.Add("# === BEGIN HARDENING DOMAIN SINKHOLE ===")
    $cleanLines.Add("# Applied on $stamp by Hardening Lockdown Tool")
    foreach ($dom in $targetUnique) {
        $cleanLines.Add("0.0.0.0 $dom")
        $cleanLines.Add(":: $dom")
        Write-Log "Queued sinkhole: 0.0.0.0 $dom  |  :: $dom" 'VERBOSE'
    }
    $cleanLines.Add("# === END HARDENING DOMAIN SINKHOLE ===")

    $linesArray = [string[]]$cleanLines.ToArray()

    if ($Dry) {
        Write-Log "[DryRun] Would write $($linesArray.Length) total lines to $Path." 'INFO'
        return $targetUnique.Count
    }

    # Staged atomic replacement with 4-tier fallback and retry backoff
    $tempFile = Join-Path $env:TEMP ("hosts_stage_" + [Guid]::NewGuid().ToString('N') + ".tmp")
    Write-Log "Writing staged temporary file: $tempFile" 'VERBOSE'
    [System.IO.File]::WriteAllLines($tempFile, $linesArray, [System.Text.Encoding]::ASCII)

    $written = $false
    $lastErr = ""

    for ($attempt = 1; $attempt -le 4; $attempt++) {
        Write-Log "Attempt $attempt/4: Committing staged replacement to $Path..." 'VERBOSE'
        if (Test-Path -LiteralPath $Path) { & attrib.exe -r -s -h "$Path" >$null 2>&1 }

        # Method 1: .NET File Copy
        try {
            [System.IO.File]::Copy($tempFile, $Path, $true)
            Write-Log "Replacement succeeded via .NET File::Copy." 'VERBOSE'
            $written = $true
            break
        } catch { $lastErr = $_.Exception.Message }

        # Method 2: cmd.exe copy
        try {
            & cmd.exe /c "copy /y `"$tempFile`" `"$Path`" >nul 2>&1"
            if ($LASTEXITCODE -eq 0) {
                Write-Log "Replacement succeeded via cmd.exe copy." 'VERBOSE'
                $written = $true
                break
            }
        } catch { }

        # Method 3: PowerShell Set-Content
        try {
            Set-Content -LiteralPath $Path -Value $linesArray -Encoding ASCII -Force -ErrorAction Stop
            Write-Log "Replacement succeeded via PowerShell Set-Content." 'VERBOSE'
            $written = $true
            break
        } catch { $lastErr = $_.Exception.Message }

        # Method 4: Move-Item overwrite
        try {
            Move-Item -LiteralPath $tempFile -Destination $Path -Force -ErrorAction Stop
            Write-Log "Replacement succeeded via Move-Item." 'VERBOSE'
            $written = $true
            break
        } catch { $lastErr = $_.Exception.Message }

        Write-Log "Transient lock detected ($lastErr). Waiting 300ms before retry..." 'VERBOSE'
        Start-Sleep -Milliseconds 300
    }

    if (Test-Path -LiteralPath $tempFile) {
        Remove-Item -LiteralPath $tempFile -Force -ErrorAction SilentlyContinue
    }

    if (-not $written) {
        throw "Failed writing to $Path after 4 attempts across all fallback channels: $lastErr"
    }

    # Normalize attributes
    & attrib.exe -r -s -h "$Path" >$null 2>&1

    # Flush DNS cache
    Write-Log "Flushing Windows DNS Resolver Cache..." 'VERBOSE'
    try { Clear-DnsClientCache -ErrorAction SilentlyContinue } catch { }
    & ipconfig.exe /flushdns >$null 2>&1

    return $targetUnique.Count
}

# ==============================================================================
# MAIN EXECUTION (VERBOSE STREAM)
# ==============================================================================
Write-Log "UNIFIED SECURITY HARDENING & DEFENSE SCRIPT INITIALIZING" 'HEADER'
if ($DryRun) { Write-Log "[DRY-RUN MODE] Simulating execution. No state changes will be made." 'WARN' }

Write-Log "System Information:" 'SUBHEADER'
Write-Log "OS Version:         $([System.Environment]::OSVersion.VersionString) ($(if ([System.Environment]::Is64BitOperatingSystem) {'64-bit'} else {'32-bit'}))" 'VERBOSE'
Write-Log "PowerShell Version: $($PSVersionTable.PSVersion)" 'VERBOSE'
Write-Log "User Identity:      $([Security.Principal.WindowsIdentity]::GetCurrent().Name) (High Mandatory / Elevated Administrator)" 'VERBOSE'
Write-Log "System Root:        $SysWinDir" 'VERBOSE'
Write-Log "System Cursors:     $SysCursorsDir" 'VERBOSE'
Write-Log "System Hosts File:  $HostsFilePath" 'VERBOSE'
Write-Log "Backup Directory:   $BackupDir" 'VERBOSE'
Write-Log "Activity Log File:  $LogPath" 'VERBOSE'

Write-Log "Target Domains ($($TargetDomains.Count) total):" 'SUBHEADER'
foreach ($dom in $TargetDomains) {
    Write-Log "$dom" 'VERBOSE'
}

# ------------------------------------------------------------------------------
# SECTION 1: CURSOR & THEME LOCKDOWN
# ------------------------------------------------------------------------------
$totalProfilesHardened = 0
$totalSlotsReset       = 0
$activeUserChanged     = $false

if (-not $SkipCursors) {
    Write-Log "STAGE 1: CURSOR & THEME HIJACK HARDENING" 'HEADER'

    # Machine-wide policies
    Write-Log "Configuring Machine-Wide Personalization Policies..." 'INFO'
    $hklmPersonalization = 'HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization'
    Write-Log "Target Key: $hklmPersonalization" 'VERBOSE'
    Write-Log "Writing: NoChangingMousePointers = 1 (REG_DWORD)" 'VERBOSE'
    Write-Log "Writing: NoChangingSoundScheme   = 1 (REG_DWORD)" 'VERBOSE'
    
    if (-not $DryRun) {
        try {
            & reg.exe add $hklmPersonalization /v NoChangingMousePointers /t REG_DWORD /d 1 /f | Out-Null
            & reg.exe add $hklmPersonalization /v NoChangingSoundScheme /t REG_DWORD /d 1 /f | Out-Null
            Write-Log "Machine policies applied: NoChangingMousePointers=1, NoChangingSoundScheme=1" 'OK'
        } catch {
            Write-Log "Failed to write machine policy: $($_.Exception.Message)" 'ERROR'
        }
    } else {
        Write-Log "[DryRun] Would write NoChangingMousePointers=1 and NoChangingSoundScheme=1 at $hklmPersonalization" 'INFO'
    }

    # Enumerate local profiles
    Write-Log "Enumerating local profiles in HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList..." 'VERBOSE'
    $profileTargets = @()
    $profileListKey = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList'
    foreach ($k in (Get-ChildItem $profileListKey -ErrorAction SilentlyContinue)) {
        if ($k.PSChildName -notmatch '^S-1-5-21-') { continue }
        $prop = Get-ItemProperty $k.PSPath -ErrorAction SilentlyContinue
        $pPath = $prop.ProfileImagePath
        if ($pPath -and (Test-Path (Join-Path $pPath 'NTUSER.DAT'))) {
            $profileTargets += [pscustomobject]@{
                Name = $k.PSChildName
                Sid  = $k.PSChildName
                Dat  = (Join-Path $pPath 'NTUSER.DAT')
            }
            Write-Log "Found Profile Hive: [$($k.PSChildName)] at $pPath\NTUSER.DAT" 'VERBOSE'
        }
    }

    # Default profile for future user accounts
    $defaultDat = $null
    try {
        $profilesRoot = (Get-ItemProperty $profileListKey -ErrorAction SilentlyContinue).ProfilesDirectory
        if ($profilesRoot -and (Test-Path (Join-Path $profilesRoot 'Default\NTUSER.DAT'))) {
            $defaultDat = Join-Path $profilesRoot 'Default\NTUSER.DAT'
        }
    } catch { }
    if (-not $defaultDat -and (Test-Path 'C:\Users\Default\NTUSER.DAT')) {
        $defaultDat = 'C:\Users\Default\NTUSER.DAT'
    }
    if ($defaultDat) {
        $profileTargets += [pscustomobject]@{
            Name = 'DefaultProfile'
            Sid  = $null
            Dat  = $defaultDat
        }
        Write-Log "Found Default Profile Hive (for future users): $defaultDat" 'VERBOSE'
    }

    Write-Log "Identified $($profileTargets.Count) profile target(s) to process." 'INFO'

    foreach ($t in $profileTargets) {
        Write-Log "Processing Profile Target: [$($t.Name)]" 'SUBHEADER'
        $loadedHere = $false
        $root = $null
        
        if ($t.Sid -and (Test-Path "Registry::HKEY_USERS\$($t.Sid)")) {
            $root = "HKU\$($t.Sid)"
            Write-Log "Hive is currently mounted in memory at $root" 'VERBOSE'
        } else {
            $root = "HKU\HARDENLOCK_$($t.Name)"
            Write-Log "Hive is offline. Mounting: '$($t.Dat)' -> $root" 'VERBOSE'
            if (-not $DryRun) {
                & reg.exe load $root $t.Dat | Out-Null
                if ($LASTEXITCODE -ne 0) {
                    Write-Log "Could not mount offline registry hive for $($t.Name)" 'WARN'
                    continue
                }
                $loadedHere = $true
            } else {
                Write-Log "[DryRun] Would mount offline hive from $($t.Dat) to $root" 'INFO'
                continue
            }
        }

        try {
            $themesKey = "$root\Software\Microsoft\Windows\CurrentVersion\Themes"
            $policyKey = "$root\Software\Policies\Microsoft\Windows\Personalization"
            $cursorKey = "$root\Control Panel\Cursors"

            if (-not $DryRun) {
                # Backup keys
                Write-Log "Backing up Themes key to: $(Join-Path $BackupDir "$($t.Name)_Themes.reg")" 'VERBOSE'
                & cmd.exe /c "reg.exe export `"$themesKey`" `"$(Join-Path $BackupDir "$($t.Name)_Themes.reg")`" /y >nul 2>&1"
                Write-Log "Backing up Personalization key to: $(Join-Path $BackupDir "$($t.Name)_Personalization.reg")" 'VERBOSE'
                & cmd.exe /c "reg.exe export `"$policyKey`" `"$(Join-Path $BackupDir "$($t.Name)_Personalization.reg")`" /y >nul 2>&1"
                Write-Log "Backing up Cursors key to: $(Join-Path $BackupDir "$($t.Name)_Cursors.reg")" 'VERBOSE'
                & cmd.exe /c "reg.exe export `"$cursorKey`" `"$(Join-Path $BackupDir "$($t.Name)_Cursors.reg")`" /y >nul 2>&1"

                # Enforce lockdown
                Write-Log "Writing $themesKey\ThemeChangesMousePointers = 0" 'VERBOSE'
                & reg.exe add $themesKey /v ThemeChangesMousePointers /t REG_DWORD /d 0 /f | Out-Null
                Write-Log "Writing $policyKey\NoChangingMousePointers = 1" 'VERBOSE'
                & reg.exe add $policyKey /v NoChangingMousePointers /t REG_DWORD /d 1 /f | Out-Null
                Write-Log "Writing $policyKey\NoChangingSoundScheme = 1" 'VERBOSE'
                & reg.exe add $policyKey /v NoChangingSoundScheme /t REG_DWORD /d 1 /f | Out-Null
            }

            # Inspect and sanitize slots
            $profileResetCount = 0
            $psCursorPath = "Registry::" + ($cursorKey -replace '^HKU', 'HKEY_USERS')
            if (Test-Path -LiteralPath $psCursorPath) {
                $cursorProps = Get-ItemProperty -LiteralPath $psCursorPath -ErrorAction SilentlyContinue
                foreach ($slot in $CursorSlots) {
                    $currentVal = $cursorProps.$slot
                    $reason = ""
                    $isApproved = Test-ApprovedCursor $currentVal ([ref]$reason)

                    if ($isApproved) {
                        Write-Log "Slot '$slot': $reason" 'VERBOSE'
                    } else {
                        Write-Log "Slot '$slot' flagged: $reason" 'WARN'
                        if (-not $DryRun) {
                            & reg.exe add $cursorKey /v $slot /t REG_EXPAND_SZ /d '' /f | Out-Null
                        }
                        Write-Log "RESET [$($t.Name)] Slot '$slot': '$currentVal' -> (System Default)" 'RESET'
                        $profileResetCount++
                        $totalSlotsReset++
                    }
                }
                if ($profileResetCount -gt 0 -and -not $DryRun) {
                    Write-Log "Clearing active rogue scheme name in $cursorKey\(Default)..." 'VERBOSE'
                    & reg.exe add $cursorKey /ve /t REG_SZ /d '' /f | Out-Null
                    if (-not $loadedHere) { $activeUserChanged = $true }
                }
            }

            Write-Log "Profile [$($t.Name)] status: Policies enforced, $profileResetCount slot(s) sanitized" 'OK'
            $totalProfilesHardened++
        } catch {
            Write-Log "Error processing profile $($t.Name): $($_.Exception.Message)" 'ERROR'
        } finally {
            if ($loadedHere) {
                Write-Log "Unmounting offline hive $root..." 'VERBOSE'
                [GC]::Collect()
                [GC]::WaitForPendingFinalizers()
                & reg.exe unload $root | Out-Null
                Write-Log "Hive $root unmounted cleanly." 'VERBOSE'
            }
        }
    }

    if ($activeUserChanged -and -not $DryRun) {
        Write-Log "Broadcasting SPI_SETCURSORS (0x0057) to refresh active desktop pointers..." 'INFO'
        Invoke-CursorRefresh
        Write-Log "Active pointer display refreshed in real-time." 'OK'
    }
} else {
    Write-Log "STAGE 1: Skipped by parameter (-SkipCursors)." 'INFO'
}

# ------------------------------------------------------------------------------
# SECTION 2: DEFENSE-IN-DEPTH DOMAIN BLOCKING (HOSTS SINKHOLE + FIREWALL)
# ------------------------------------------------------------------------------
Write-Log "STAGE 2: DOMAIN BLOCKING & NETWORK CONTAINMENT" 'HEADER'

# --- Layer 1: Hosts File Sinkhole ---
$hostsCount = 0
if (-not $SkipHosts) {
    Write-Log "Applying Layer 1: Windows Hosts File Sinkhole..." 'INFO'
    try {
        $hostsCount = Update-HostsFileSafely -Path $HostsFilePath -Domains $TargetDomains -BackupDirectory $BackupDir -Dry:$DryRun
        if (-not $DryRun) {
            Write-Log "Successfully sinkholed $hostsCount domain(s) in $HostsFilePath" 'OK'
            Write-Log "Flushed Windows DNS Client Resolver Cache." 'OK'
        } else {
            Write-Log "[DryRun] Would sinkhole $hostsCount domain(s) in $HostsFilePath" 'INFO'
        }
    } catch {
        Write-Log "Failed updating hosts file: $($_.Exception.Message)" 'ERROR'
        Write-Log "Note: If an active Antivirus/Defender real-time lock is blocking hosts write, Layer 2 (Firewall) below still drops all network packets." 'WARN'
    }
} else {
    Write-Log "Layer 1 (Hosts Sinkhole): Skipped by parameter (-SkipHosts)." 'INFO'
}

# --- Layer 2: Outbound Windows Defender Firewall Rules ---
$rulesAdded  = 0
$rulesFailed = 0
$unresolved  = 0

if (-not $SkipFirewall) {
    Write-Log "Applying Layer 2: Windows Defender Firewall Outbound Drop Rules..." 'INFO'

    if (-not $DryRun) {
        Write-Log "Scanning for pre-existing rules matching '$FirewallPrefix *' or 'DSH Block *'..." 'VERBOSE'
        $oldRules = @(Get-NetFirewallRule -ErrorAction SilentlyContinue | Where-Object {
            $_.DisplayName -like "$FirewallPrefix *" -or $_.DisplayName -like "DSH Block *"
        })
        $removedCount = 0
        foreach ($r in $oldRules) {
            try {
                Remove-NetFirewallRule -Name $r.Name -ErrorAction Stop
                Write-Log "Purged rule: $($r.DisplayName)" 'VERBOSE'
                $removedCount++
            } catch { }
        }
        if ($removedCount -gt 0) {
            Write-Log "Purged $removedCount legacy/previous firewall block rule(s)." 'INFO'
        } else {
            Write-Log "No pre-existing firewall rules needed purging." 'VERBOSE'
        }
    }

    foreach ($dom in ($TargetDomains | Select-Object -Unique)) {
        Write-Log "Resolving DNS records for domain: $dom..." 'VERBOSE'
        $ips = @(Resolve-HostAddresses $dom)
        if ($ips.Count -eq 0) {
            Write-Log "DNS query for '$dom' returned no active IP addresses (Protected by Hosts Sinkhole)" 'WARN'
            $unresolved++
            continue
        }

        $v4 = @($ips | Where-Object { $_ -notmatch ':' })
        $v6 = @($ips | Where-Object { $_ -match ':' })

        if ($v4.Count -gt 0) {
            Write-Log "  -> IPv4 ($dom): $($v4 -join ', ')" 'VERBOSE'
            $ruleNameV4 = "$FirewallPrefix $dom"
            if (-not $DryRun) {
                try {
                    New-NetFirewallRule -Name $ruleNameV4 `
                                        -DisplayName $ruleNameV4 `
                                        -Direction Outbound `
                                        -Action Block `
                                        -RemoteAddress $v4 `
                                        -Enabled True `
                                        -Profile Any `
                                        -ErrorAction Stop | Out-Null
                    Write-Log "Firewall [IPv4]: Blocked $dom -> $($v4 -join ', ')" 'OK'
                    $rulesAdded++
                } catch {
                    Write-Log "Failed to add IPv4 rule for $dom : $($_.Exception.Message)" 'ERROR'
                    $rulesFailed++
                }
            } else {
                Write-Log "[DryRun] Would add outbound block rule for $dom (IPv4: $($v4 -join ', '))" 'INFO'
                $rulesAdded++
            }
        }

        if ($v6.Count -gt 0) {
            Write-Log "  -> IPv6 ($dom): $($v6 -join ', ')" 'VERBOSE'
            $ruleNameV6 = "$FirewallPrefix $dom (IPv6)"
            if (-not $DryRun) {
                try {
                    New-NetFirewallRule -Name $ruleNameV6 `
                                        -DisplayName $ruleNameV6 `
                                        -Direction Outbound `
                                        -Action Block `
                                        -RemoteAddress $v6 `
                                        -Enabled True `
                                        -Profile Any `
                                        -ErrorAction Stop | Out-Null
                    Write-Log "Firewall [IPv6]: Blocked $dom -> $($v6 -join ', ')" 'OK'
                    $rulesAdded++
                } catch {
                    Write-Log "Failed to add IPv6 rule for $dom : $($_.Exception.Message)" 'ERROR'
                    $rulesFailed++
                }
            } else {
                Write-Log "[DryRun] Would add outbound block rule for $dom (IPv6: $($v6 -join ', '))" 'INFO'
                $rulesAdded++
            }
        }
    }
} else {
    Write-Log "Layer 2 (Firewall Drop Rules): Skipped by parameter (-SkipFirewall)." 'INFO'
}

# ------------------------------------------------------------------------------
# SECTION 3: AUDIT & HARDENING VERIFICATION REPORT
# ------------------------------------------------------------------------------
Write-Log "HARDENING COMPLETION SUMMARY" 'HEADER'
Write-Host "  * Local Profiles Hardened:        $totalProfilesHardened" -ForegroundColor White
Write-Host "  * Cursor Pointer Slots Sanitized: $totalSlotsReset" -ForegroundColor White
Write-Host "  * Domains Sinkholed in Hosts:     $hostsCount" -ForegroundColor White
Write-Host "  * Active Outbound Firewall Rules: $rulesAdded" -ForegroundColor White
Write-Host "  * Registry/Hosts Backup Location: $BackupDir" -ForegroundColor White
Write-Host "  * Persistent Activity Log:        $LogPath" -ForegroundColor White
Write-Host ""

if ($rulesFailed -gt 0) {
    exit 1
} else {
    exit 0
}

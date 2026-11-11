Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!ifndef VERSION
  !define VERSION "r16"
!endif
!ifndef SRC
  !define SRC "publish"
!endif
Name "K-Meleon-R"
OutFile "K-Meleon-R-${VERSION}-x64-setup.exe"
; "highest": elevates when the account can, but still allows plain user installs.
; Without elevation the Windows policy hardening steps are skipped (app install is per-user anyway).
RequestExecutionLevel highest
InstallDir "$LOCALAPPDATA\Programs\K-Meleon-R"
InstallDirRegKey HKCU "Software\K-Meleon-R" "InstallDir"

; Installer metadata (imortalimp.nl)
VIProductVersion "8.8.0.0"
VIAddVersionKey "ProductName" "K-Meleon-R"
VIAddVersionKey "ProductVersion" "${VERSION}"
VIAddVersionKey "FileVersion" "8.8.0.0"
VIAddVersionKey "FileDescription" "K-Meleon-R hardened WebView2 browser installer"
VIAddVersionKey "CompanyName" "imortalimp.nl"
VIAddVersionKey "LegalCopyright" "River Lyle Reuveni (TheImortalimp) - imortalimp.nl"

!insertmacro MUI_PAGE_WELCOME
; License and security notices: .NET MIT tribute plus the SmartScreen/admin warning.
!define MUI_PAGE_HEADER_TEXT "License and security notices"
!define MUI_PAGE_HEADER_SUBTEXT "Please review the .NET license tribute and the SmartScreen/administrator notice."
!insertmacro MUI_PAGE_LICENSE "installer-notices.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\K-Meleon-R.exe"
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Var IsAdmin

Function .onInit
  UserInfo::GetAccountType
  Pop $IsAdmin
  ${If} $IsAdmin == "Admin"
    MessageBox MB_OKCANCEL|MB_ICONEXCLAMATION \
      "SmartScreen / administrator notice:$\r$\n$\r$\nThis installer is not code-signed, so Microsoft Defender SmartScreen may warn about it. Verify the SHA-256 checksum from the GitHub release page.$\r$\n$\r$\nYou are installing with administrator rights: Windows pointer/theme/sound policy hardening will be applied, and a hidden 'K-Meleon-R Hourly Hardening' scheduled task will run the GEM-OS lockdown engine silently every hour. The policy hardening is not undone by uninstalling; the scheduled task is removed on uninstall.$\r$\n$\r$\nContinue?" \
      IDOK +2
    Abort
  ${Else}
    MessageBox MB_OKCANCEL|MB_ICONINFORMATION \
      "SmartScreen notice:$\r$\n$\r$\nThis installer is not code-signed, so Microsoft Defender SmartScreen may warn about it. Verify the SHA-256 checksum from the GitHub release page.$\r$\n$\r$\nYou are installing without administrator rights: the browser installs per-user and the Windows policy hardening steps will be skipped.$\r$\n$\r$\nContinue?" \
      IDOK +2
    Abort
  ${EndIf}
FunctionEnd

Section "Install"
  SetOutPath "$INSTDIR"
  File /r "${SRC}\*.*"
  File "cursor_lockdown.ps1"
  File "harden_against_gem_os.ps1"
  File "Hardening_Lockdown_Standalone.bat"
  File "install_hourly_hardening.ps1"
  File "install-flint.ps1"
  File "installer-notices.txt"
  ${If} $IsAdmin == "Admin"
    DetailPrint "Locking mouse pointers to the Windows system set..."
    nsExec::ExecToLog '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy RemoteSigned -File "$INSTDIR\cursor_lockdown.ps1"'
    Pop $0
    DetailPrint "Applying GEM OS theme/cursor hardening..."
    nsExec::ExecToLog '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy RemoteSigned -File "$INSTDIR\harden_against_gem_os.ps1" -Apply'
    Pop $0
    DetailPrint "Registering the hourly silent hardening task..."
    nsExec::ExecToLog '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy RemoteSigned -File "$INSTDIR\install_hourly_hardening.ps1" -ScriptPath "$INSTDIR\Hardening_Lockdown_Standalone.bat"'
    Pop $0
  ${Else}
    DetailPrint "No administrator rights: Windows policy hardening and the hourly hardening task were skipped (run the installed scripts elevated later if wanted)."
  ${EndIf}
  WriteUninstaller "$INSTDIR\uninstall.exe"
  CreateDirectory "$SMPROGRAMS\K-Meleon-R"
  CreateShortcut "$SMPROGRAMS\K-Meleon-R\K-Meleon-R.lnk" "$INSTDIR\K-Meleon-R.exe"
  CreateShortcut "$SMPROGRAMS\K-Meleon-R\Uninstall.lnk" "$INSTDIR\uninstall.exe"
  CreateShortcut "$DESKTOP\K-Meleon-R.lnk" "$INSTDIR\K-Meleon-R.exe"
  WriteRegStr HKCU "Software\K-Meleon-R" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "DisplayName" "K-Meleon-R"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "Publisher" "TheImortalimp (imortalimp.nl)"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "URLInfoAbout" "https://imortalimp.nl"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "HelpLink" "https://github.com/TheImortalimp/K-Meleon-R"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "UninstallString" "$INSTDIR\uninstall.exe"
SectionEnd

Section "Uninstall"
  nsExec::ExecToLog '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy RemoteSigned -File "$INSTDIR\install_hourly_hardening.ps1" -Remove'
  Pop $0
  RMDir /r "$INSTDIR"
  Delete "$DESKTOP\K-Meleon-R.lnk"
  RMDir /r "$SMPROGRAMS\K-Meleon-R"
  DeleteRegKey HKCU "Software\K-Meleon-R"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R"
SectionEnd

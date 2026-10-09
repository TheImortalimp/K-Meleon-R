Unicode true
!include "MUI2.nsh"
!ifndef VERSION
  !define VERSION "r7"
!endif
!ifndef SRC
  !define SRC "publish"
!endif
Name "K-Meleon-R"
OutFile "K-Meleon-R-${VERSION}-x64-setup.exe"
RequestExecutionLevel user
InstallDir "$LOCALAPPDATA\Programs\K-Meleon-R"
InstallDirRegKey HKCU "Software\K-Meleon-R" "InstallDir"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\K-Meleon-R.exe"
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Section "Install"
  SetOutPath "$INSTDIR"
  File /r "${SRC}\*.*"
  WriteUninstaller "$INSTDIR\uninstall.exe"
  CreateDirectory "$SMPROGRAMS\K-Meleon-R"
  CreateShortcut "$SMPROGRAMS\K-Meleon-R\K-Meleon-R.lnk" "$INSTDIR\K-Meleon-R.exe"
  CreateShortcut "$SMPROGRAMS\K-Meleon-R\Uninstall.lnk" "$INSTDIR\uninstall.exe"
  CreateShortcut "$DESKTOP\K-Meleon-R.lnk" "$INSTDIR\K-Meleon-R.exe"
  WriteRegStr HKCU "Software\K-Meleon-R" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "DisplayName" "K-Meleon-R"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "Publisher" "River Lyle Reuveni (TheImortalimp)"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "UninstallString" "$INSTDIR\uninstall.exe"
SectionEnd

Section "Uninstall"
  RMDir /r "$INSTDIR"
  Delete "$DESKTOP\K-Meleon-R.lnk"
  RMDir /r "$SMPROGRAMS\K-Meleon-R"
  DeleteRegKey HKCU "Software\K-Meleon-R"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R"
SectionEnd

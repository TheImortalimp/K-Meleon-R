Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!ifndef VERSION
  !define VERSION "r6"
!endif
Name "K-Meleon-R Start Page for Pale Moon"
OutFile "K-Meleon-R-${VERSION}-PaleMoon-setup.exe"
RequestExecutionLevel user
InstallDir "$LOCALAPPDATA\K-Meleon-R"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Var PM

Section "Install"
  SetOutPath "$INSTDIR"
  File "kmeleon-r-startpage.xpi"
  File "NOTICE-K-Meleon-R.md"
  File "README.md"
  WriteUninstaller "$INSTDIR\uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "DisplayName" "K-Meleon-R Start Page for Pale Moon"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R" "UninstallString" "$INSTDIR\uninstall.exe"

  StrCpy $PM ""
  ReadRegStr $PM HKLM "Software\Microsoft\Windows\CurrentVersion\App Paths\palemoon.exe" ""
  ${If} $PM == ""
    ReadRegStr $PM HKCU "Software\Microsoft\Windows\CurrentVersion\App Paths\palemoon.exe" ""
  ${EndIf}
  ${If} $PM == ""
  ${AndIf} ${FileExists} "$PROGRAMFILES64\Pale Moon\palemoon.exe"
    StrCpy $PM "$PROGRAMFILES64\Pale Moon\palemoon.exe"
  ${EndIf}
  ${If} $PM == ""
  ${AndIf} ${FileExists} "$PROGRAMFILES32\Pale Moon\palemoon.exe"
    StrCpy $PM "$PROGRAMFILES32\Pale Moon\palemoon.exe"
  ${EndIf}

  ${If} $PM == ""
    MessageBox MB_OK|MB_ICONINFORMATION "Pale Moon was not found.$\n$\nInstall Pale Moon from https://www.palemoon.org/ and then open:$\n$INSTDIR\kmeleon-r-startpage.xpi"
    ExecShell "open" "https://www.palemoon.org/"
  ${Else}
    Exec '"$PM" "$INSTDIR\kmeleon-r-startpage.xpi"'
  ${EndIf}
SectionEnd

Section "Uninstall"
  Delete "$INSTDIR\*.*"
  RMDir "$INSTDIR"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\K-Meleon-R"
SectionEnd

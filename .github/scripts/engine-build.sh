#!/bin/bash
# Runs inside MozillaBuild msys; invoked by build-engine.yml
cd "$(cygpath -u "$GITHUB_WORKSPACE")/goanna" || exit 1
export PATH="$PATH:/c/mozilla-build/python:/c/mozilla-build/python27"
python --version
if [ "$STAGE" = asmtest ]; then
  cd media/libvpx || exit 1
  A=third_party/x86inc/x86inc.asm
  echo "--- A msys path, with -Zi"; /c/mozilla-build/yasm/yasm.exe -o /tmp/a.obj -Zi -f x64 -rnasm -pnasm -I. -Ivpx_ports/ $A; echo rc=$?
  echo "--- B no -Zi"; /c/mozilla-build/yasm/yasm.exe -o /tmp/b.obj -f x64 -rnasm -pnasm -I. -Ivpx_ports/ $A; echo rc=$?
  echo "--- C C:/yasm"; /c/yasm/yasm.exe -o /tmp/c.obj -Zi -f x64 -rnasm -pnasm -I. -Ivpx_ports/ $A; echo rc=$?
  echo "--- D via cmd"; cmd /c "C:\\yasm\\yasm.exe -o c:\\d.obj -f x64 -rnasm -pnasm -I. -Ivpx_ports/ $A"; echo rc=$?
  echo "--- E mozilla yasm version"; /c/mozilla-build/yasm/yasm.exe --version; echo rc=$?
  ls -la /c/mozilla-build/yasm /c/yasm
  exit 0
fi
which yasm; yasm --version; echo yasm-rc=$?; /c/yasm/yasm.exe --version | head -1; echo yasm2-rc=$?; ls -la /c/yasm; echo PATH=$PATH | tr ':' '\n' | head -30
if [ "$STAGE" = build ]; then
  python mach build
  rc=$?
  if [ $rc -ne 0 ]; then
    echo "=== BUILD FAILED, isolating errors with -j1 ==="
    for d in $RETRY_DIRS; do
      echo "=== retry $d ==="
      python mach build -j1 $d > /tmp/retry.log 2>&1
      grep -v 'warning' /tmp/retry.log | grep -E 'error|Error|\*\*\*' | head -n 40
      if [ -n "$INCLUDE_TRACE" ] && [ "$d" = "$INCLUDE_TRACE" ]; then
        line=$(grep -m1 'mozbuild.action.cl cl -Fo' /tmp/retry.log)
        objdir=$(grep -m1 'In the directory' /tmp/retry.log | sed 's/.*directory *//')
        if [ -n "$line" ]; then
          echo "=== include trace for $d ==="
          (cd "$objdir" && eval "cl ${line#*mozbuild.action.cl cl } -showIncludes" 2>&1 | sed -n '1,/error C/p' | tail -n 60)
        fi
      fi
    done
  fi
  if [ $rc -ne 0 ]; then
    echo '=== asm diagnostics ==='
    cd obj-x64/media/libvpx && rm -f x86inc.obj && mozmake x86inc.obj V=1 2>&1 | head -n 15
    grep -E '^(AS|ASFLAGS|AS_DASH_C_FLAG) *=' backend.mk Makefile ../../config/autoconf.mk 2>/dev/null | head
    grep -E '^(AS|ASFLAGS) *=' ../../config/autoconf.mk | head
  fi
  exit $rc
else
  python mach configure
fi
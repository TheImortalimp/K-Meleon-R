#!/bin/bash
# Runs inside MozillaBuild msys; invoked by build-engine.yml
cd "$(cygpath -u "$GITHUB_WORKSPACE")/goanna" || exit 1
export PATH="$PATH:/c/mozilla-build/python:/c/mozilla-build/python27"
python --version
which yasm; yasm --version; echo yasm-rc=$?; ls -la /c/mozilla-build/yasm 2>&1 | head
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
  exit $rc
else
  python mach configure
fi
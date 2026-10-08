#!/bin/bash
# Runs inside MozillaBuild msys; invoked by build-engine.yml
cd "$(cygpath -u "$GITHUB_WORKSPACE")/goanna" || exit 1
export PATH="$PATH:/c/mozilla-build/python:/c/mozilla-build/python27"
python --version
if [ "$STAGE" = build ]; then
  python mach build
  rc=$?
  if [ $rc -ne 0 ]; then
    echo "=== BUILD FAILED, isolating errors with -j1 ==="
    for d in $RETRY_DIRS; do
      echo "=== retry $d ==="
      python mach build -j1 $d 2>&1 | grep -v 'warning' | grep -E 'error|Error|\*\*\*' | head -n 40
    done
  fi
  exit $rc
else
  python mach configure
fi
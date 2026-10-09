#!/bin/bash
# Runs inside MozillaBuild msys; invoked by build-uxp.yml
cd "$(cygpath -u "$GITHUB_WORKSPACE")/goanna" || exit 1
export PATH="$PATH:/c/mozilla-build/python:/c/mozilla-build/python2"
export PATH="$(echo "$PATH" | tr ':' '\n' | grep -vi strawberry | paste -sd:)"
which python python2 python2.7; python --version
if [ "$STAGE" = build ]; then
  python ./mach build
else
  python ./mach configure
fi

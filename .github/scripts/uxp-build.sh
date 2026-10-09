#!/bin/bash
# Runs inside MozillaBuild msys; invoked by build-uxp.yml
cd "$(cygpath -u "$GITHUB_WORKSPACE")/goanna" || exit 1
export PATH="$PATH:/c/mozilla-build/python:/c/mozilla-build/python2"
export PATH="$(echo "$PATH" | tr ':' '\n' | grep -vi strawberry | paste -sd:)"
sed -i -e 's/^MOZ_EXTENSIONS_DEFAULT=.*/MOZ_EXTENSIONS_DEFAULT=""/' kmeleon/confvars.sh
export PATH="/c/yasm:/c/mozilla-build/yasm:$PATH"
export YASM=/c/yasm/yasm.exe
which yasm; yasm --version | head -1
which python python2 python2.7; python --version
if [ "$STAGE" = build ]; then
  python ./mach build
else
  python ./mach configure
fi

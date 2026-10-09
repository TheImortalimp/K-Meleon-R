#!/bin/bash
# Runs inside MozillaBuild msys; invoked by build-uxp.yml
cd "$(cygpath -u "$GITHUB_WORKSPACE")/goanna" || exit 1
export PATH="$PATH:/c/mozilla-build/python:/c/mozilla-build/python2"
export PATH="$(echo "$PATH" | tr ':' '\n' | grep -vi strawberry | paste -sd:)"
sed -i -e 's/^MOZ_EXTENSIONS_DEFAULT=.*/MOZ_EXTENSIONS_DEFAULT=""/' kmeleon/confvars.sh
export PATH="/c/yasm:/c/mozilla-build/yasm:$PATH"
export YASM='c:/mozilla-build/yasm/yasm.exe'
cp -f /c/yasm/yasm.exe /c/mozilla-build/bin/yasm.exe; cp -f /c/yasm/yasm.exe /c/mozilla-build/msys/bin/yasm.exe
cp -f /c/yasm/yasm.exe /c/Windows/yasm.exe; cp -f /c/yasm/yasm.exe /c/Windows/System32/yasm.exe
sed -i "s|^yasm = check_prog('YASM'.*|@depends('--help')\ndef yasm(_):\n    return 'C:/mozilla-build/yasm/yasm.exe'\nadd_old_configure_assignment('YASM', yasm)\nset_config('YASM', yasm)|" build/moz.configure/toolchain.configure
grep -n -B1 -A3 "^def yasm(_)" build/moz.configure/toolchain.configure
sed -i -e "/^GENERATED_INCLUDES += \[/,/^\]/{s/GENERATED_INCLUDES/LOCAL_INCLUDES/;s|'/build'|'!/build'|}" kmeleon/app/moz.build
find kmeleon -name moz.build -exec sed -i -e "/^[[:space:]]*'mozalloc',[[:space:]]*$/d" {} +
cat > kmeleon/app/application.ini <<'EOF'
#filter substitution
[App]
Vendor=@MOZ_APP_VENDOR@
Name=@MOZ_APP_BASENAME@
RemotingName=@MOZ_APP_NAME@
Version=@MOZ_APP_VERSION@
BuildID=@MOZ_BUILDID@
ID=@MOZ_APP_ID@

[Gecko]
MinVersion=@GRE_MILESTONE@
MaxVersion=@GRE_MILESTONE@

[XRE]
EnableProfileMigrator=1
EOF
which yasm; yasm --version | head -1
which python python2 python2.7; python --version
if [ "$STAGE" = build ]; then
  python ./mach build
else
  python ./mach configure
fi

import os
import zipfile

here = os.path.dirname(os.path.abspath(__file__))
out = os.path.join(here, "kmeleon-r-startpage.xpi")
skip = {"build.py", "kmeleon-r-startpage.xpi", "README.md"}

with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
    for root, _, files in os.walk(here):
        for f in files:
            if f in skip and root == here:
                continue
            p = os.path.join(root, f)
            z.write(p, os.path.relpath(p, here).replace("\\", "/"))
print(out)

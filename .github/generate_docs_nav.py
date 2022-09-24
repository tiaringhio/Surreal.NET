#!/bin/env python

import os

root = "docs"

def process_dir(directory, index, indent):
    result = indent + "- \"" + os.path.basename(directory) + "\":\n"

    indent += "  "
    if index is not None:
        result += indent + "- \"" + index[5:] + "\"\n"
    
    md = False
    for f in sorted(os.listdir(directory)):
        f = os.path.join(directory, f)
        if os.path.isfile(f):
            if f.endswith(".md"):
                md = True

                if os.path.isdir(f[:-3]):
                    result += process_dir(f[:-3], f, indent)
                else:
                    result += indent + "- \"" + os.path.basename(f)[:-3] + "\": \"" + f[5:] + "\"\n"

    if md:
        return result
    else:
        return ""

nav = {}

package_dirs = dict([ (f.split(".")[-1], f) for f in os.listdir(root) if os.path.isdir(os.path.join(root, f)) ])
for f in sorted(os.listdir(root)):
    f_path = os.path.join(root, f)
    if os.path.isfile(f_path):
        if f.endswith(".md"):
            title = f[:-3]
            if title in package_dirs:
                directory = package_dirs[title]
                nav[directory] = process_dir(os.path.join(root, directory), f_path, "  ")
                package_dirs.pop(title)
            else:
                title = "Home" if f == "index.md" else title
                nav[title] = "  - \"" + title + "\": \"" + f + "\"\n"

for title, directory in package_dirs.items():
    nav[os.path.basename(directory)] = process_dir(os.path.join(root, directory), None, "  ")

result = "nav:\n"

for e in sorted(nav):
    result += nav[e]

print(result)

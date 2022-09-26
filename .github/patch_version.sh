#!/bin/env bash
# This script is used to update the patch versions of the project.

# Get the current version tag from the argument
cur_ver=$1
# Convert the tag to a SemVer compatbile version
cur_ver=${cur_ver#v}

# Read the version from src/Directory.Build.props
# `<Version>1.0.4</Version>`. We later need to update the property
ver=$(grep -oPm1 "(?<=<Version>)[^<]+" src/Directory.Build.props)

# The version is defined in src/Directory.Build.props as a property
echo "Patching project version from $ver to $cur_ver"
sed -i "s/<Version>.*<\/Version>/<Version>$cur_ver<\/Version>/" src/Directory.Build.props

# The version is also defined in the README.md
# Replace the version in `Version="1.0.3"` with the current version
echo "Patching README.md version from $ver to $cur_ver"
sed -i "s/Version=\".*\"/Version=\"$cur_ver\"/" README.md

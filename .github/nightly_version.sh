#!/bin/env bash

# Obtain the latest version from the stable branch
ver=$(grep -oPm1 "(?<=<Version>)[^<]+" src/Directory.Build.props)

# Generate the nightly version
ver="v$ver-nightly$(date +%Y%m%d)"
export NIGHTLY_TAG=$ver
# Set environment variable for use in the next step
echo "NIGHTLY_TAG=$ver" >>$GITHUB_ENV

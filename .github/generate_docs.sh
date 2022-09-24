#!/bin/env bash
# Generates documentation files to docs/ from published compilations in publish/

# list of all projects
declare -a projs=(
  "Abstractions"
  "Common"
  "Configuration"
  "Driver/Rest"
  "Driver/Rpc"
  "Extensions/Service"
  "Json"
  "Models"
  "Ws"
)

cp ./README.md ./docs/index.md

# To keep things clean we will work in tmp
mkdir 'tmp'
pushd 'tmp' || exit 1

for val in "${projs[@]}"; do
  stuff=($(echo $val | tr "/" "\n"))
  last=${stuff[${#stuff[@]} - 1]}

  extract "../publish/$last.pdb" meta.json src/
  mapper meta.json src/ symbols.json
  XmlDocMdSymbols "../publish/$last.dll" ../docs/ --source https://github.com/ProphetLamb/Surreal.Net/tree/master/ --symbols symbols.json
done

popd || exit 1
rm -rf 'tmp'

#!/bin/bash

set -e

declare -a StringArray=(
  "Abstractions"
  # "Common"
  # "Configuration"
  # "Driver/Rest"
  # "Driver/Rpc"
  # "Extensions/Service"
  # "Json"
  # "Models"
  # "Ws"
)


for val in ${StringArray[@]}; do
  stuff=($(echo $val | tr "/" "\n"))
  last=${stuff[${#stuff[@]}-1]}
  path=./src/$val/bin/Release/net60/publish

  echo "Running: $path/$last.pdb $last-meta.json src/"
  extract $path/$last.pdb $last-meta.json src/

  echo "Running: mapper $last-meta.json src/ $last-symbols.json"
  mapper $last-meta.json src/ $last-symbols.json

  echo "Running: xmldocmd $path/$last.dll ./docs/src --source src/ --symbols $last-symbols.json"
  xmldocmd $path/$last.dll ./docs/src --source src/ --symbols $last-symbols.json
done

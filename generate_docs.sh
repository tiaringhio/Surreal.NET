#!/bin/bash

declare -a StringArray=(
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


for val in ${StringArray[@]}; do
  stuff=($(echo $val | tr "/" "\n"))
  last=${stuff[${#stuff[@]}-1]}
  path=./src/$val/bin/Release/net60/publish

  extract $path/$last.pdb $last-meta.json src/
  mapper $last-meta.json src/ $last-symbols.json
  xmldocmd $path/$last.dll ./docs/src --source src/ --symbols $last-symbols.json
done

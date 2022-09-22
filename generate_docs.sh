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
  xmldocmd ./src/$val/bin/Release/net60/publish/$last.dll ./docs/src --source https://github.com/ProphetLamb/Surreal.Net/tree/master/src/$val
done

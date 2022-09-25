#!/bin/env bash
# Generates documentation files to docs/ from published compilations in publish/

# list of all projects
declare -a projs=(
  "SurrealDB.Abstractions"
  "SurrealDB.Common"
  "SurrealDB.Configuration"
  "SurrealDB.Driver.Rest"
  "SurrealDB.Driver.Rpc"
  "SurrealDB.Extensions.Service"
  "SurrealDB.Json"
  "SurrealDB.Models"
  "SurrealDB.Ws"
)

cp ./README.md ./docs/index.md

# To keep things clean we will work in tmp
mkdir 'tmp'
pushd 'tmp' || exit 1

for val in "${projs[@]}"; do
  extract "../publish/$val.pdb" meta.json src/
  mapper meta.json src/ symbols.json
  XmlDocMdSymbols "../publish/$val.dll" ../docs/ --source https://github.com/ProphetLamb/Surreal.Net/tree/master/ --symbols symbols.json
done

popd || exit 1
rm -rf 'tmp'

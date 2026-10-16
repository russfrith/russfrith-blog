#!/bin/sh
# Usage: generate.sh <output-path>
# Reads ../../api/openapi.json and writes TypeScript types to the given path.
set -eu

output="${1:?usage: generate.sh <output-path>}"

npx openapi-typescript ../../api/openapi.json \
  --properties-required-by-default \
  -o "$output"

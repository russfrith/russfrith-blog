#!/bin/bash
set -u
npm init -y >/dev/null 2>&1
node -e 'const fs=require("fs");const p=JSON.parse(fs.readFileSync("package.json"));p.type="module";fs.writeFileSync("package.json",JSON.stringify(p));'
npm install --no-save typescript@5.9 @types/node@24 >/dev/null 2>&1

FILES=(
  "01_absent_null/broken.ts" "01_absent_null/fixed.ts"
  "02_data_race/broken.ts" "02_data_race/fixed.ts"
  "03_integer_overflow/demo.ts"
  "04_ignored_errors/broken.ts" "04_ignored_errors/fixed.ts"
  "05_resource_cleanup/broken.ts" "05_resource_cleanup/fixed.ts"
  "07_cpu_heavy_blocking/broken.ts" "07_cpu_heavy_blocking/fixed.ts"
  "08_untrusted_json/broken.ts" "08_untrusted_json/fixed.ts"
  "footgun_type_erasure/broken.ts" "footgun_type_erasure/fixed.ts"
)

for f in "${FILES[@]}"; do
  echo "=== $f ==="
  npx tsc --noEmit --strict --target ES2023 --module NodeNext --moduleResolution NodeNext --esModuleInterop --skipLibCheck --types node "$f"
  echo "EXIT:$?"
done

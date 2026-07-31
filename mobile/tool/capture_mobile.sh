#!/usr/bin/env bash
#
# Regenerates the mobile design sheet and copies it into docs/screenshots/mobile/.
#
# Mobile is captured by a golden rather than by a browser, because `flutter build web` cannot
# compile these apps: drift's SQLite reaches dart:ffi, which does not exist on web. Building a
# second WASM storage path purely so a screenshot tool has a target would be a lot of production
# code in service of a picture.
#
# The golden is the better artefact anyway. It is deterministic, it fails the build when the design
# changes without anyone meaning it to, and it is what caught the two defects that shipped with the
# first version of this workspace: no font assets were bundled at all, and JetBrains Mono has no
# rupee glyph, so every price rendered as a tofu box.
#
# Usage: mobile/tool/capture_mobile.sh
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mobile="$(dirname "$here")"
repo="$(dirname "$mobile")"

golden="$mobile/packages/autohub_ui/test/goldens/design_sheet.png"
out="$repo/docs/screenshots/mobile"

echo "Rendering the design sheet…"
(cd "$mobile/packages/autohub_ui" && flutter test test/design_sheet_golden_test.dart --update-goldens)

mkdir -p "$out"
cp "$golden" "$out/design-system.png"

echo "→ docs/screenshots/mobile/design-system.png"

#!/usr/bin/env bash
#
# Runs every test suite in the workspace, in dependency order.
#
# `docs/10-mobile-architecture.md` §9 names `cd mobile && flutter test` as the gate. That command
# does not do what it appears to: a pub workspace shares one resolution and one `.dart_tool`, but
# Flutter has no workspace-wide test runner, so at the root it looks for `mobile/test/`, does not
# find one, and exits non-zero with "Test directory \"test\" not found." — which reads like a broken
# checkout rather than a missing runner.
#
# Aggregating the suites into a single root package was the other option and it is the wrong one:
# it would put `flutter_test` and all six packages into the root pubspec, and it would run drift,
# secure-storage and path_provider fakes from six suites in one process, where a binding one suite
# installs leaks into the next. The suites are isolated today and that isolation is worth more than
# a single `flutter test` invocation.
#
# Dependency order, not alphabetical: core before the packages that consume it, packages before the
# apps. A break in `autohub_core` surfaces as one failure here rather than as the same failure
# repeated across four downstream suites.
set -euo pipefail

cd "$(dirname "$0")/.."

SUITES=(
  packages/autohub_core
  packages/autohub_ui
  packages/autohub_forms
  apps/customer
  apps/dealer
  apps/partner
)

failed=()

for suite in "${SUITES[@]}"; do
  printf '\n\033[1m── %s ──\033[0m\n' "$suite"
  # `|| failed+=(...)` rather than letting `set -e` abort: a repair pass wants the whole board, not
  # the first red square. The exit code at the bottom still fails the gate.
  if ! (cd "$suite" && flutter test "$@"); then
    failed+=("$suite")
  fi
done

printf '\n'
if [ ${#failed[@]} -eq 0 ]; then
  printf '\033[32mAll %d suites passed.\033[0m\n' "${#SUITES[@]}"
  exit 0
fi

printf '\033[31m%d of %d suites failed:\033[0m\n' "${#failed[@]}" "${#SUITES[@]}"
printf '  %s\n' "${failed[@]}"
exit 1

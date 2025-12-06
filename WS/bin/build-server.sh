#!/bin/sh
set -eu

SCRIPT_DIR="$(cd "$(dirname "$0")" >/dev/null 2>&1 && pwd)"
WS_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd $WS_ROOT

./gradlew build -x test -x spotlessCheck

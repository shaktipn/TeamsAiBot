#!/bin/sh
set -eu

SCRIPT_DIR="$(cd "$(dirname "$0")" >/dev/null 2>&1 && pwd)"
WS_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

sh "$WS_ROOT/bin/build-server.sh"
sh "$WS_ROOT/bin/start-server.sh"

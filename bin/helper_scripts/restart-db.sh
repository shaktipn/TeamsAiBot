#!/bin/sh

cd "$(dirname "$0")" >/dev/null 2>&1 || {
	echo "Error: Unable to change directory." >&2
	exit 1
}
SCRIPTPATH="$(pwd)"

"$SCRIPTPATH"/stop-dev-docker.sh
"$SCRIPTPATH"/start-dev-docker.sh

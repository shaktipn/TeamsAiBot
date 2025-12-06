#!/bin/sh

set -eu

SCRIPTPATH="$(
	cd -- "$(dirname "$0")" >/dev/null 2>&1
	pwd -P
)"

cd "$SCRIPTPATH"/..

java -jar "$SCRIPTPATH"/Eagle-Gen.jar bootstrap \
	--use-eagle-rpc-gen --publish None \
	--github-repo SuryaDigital/None --package-name com.suryadigital.teamsaibot --fe-package-name teamsaibot

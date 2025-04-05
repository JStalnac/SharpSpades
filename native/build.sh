#!/bin/sh

fail() {
	echo "$0: $*" >&2
	exit 1
}

CC=cc
CFLAGS="-std=c23 -Wall -Wextra -Wpedantic"
LDFLAGS="-fPIC -lm"
SOURCES="player.c grenade.c map.c hit_detection.c line.c"
output="bin"

for arg ; do
	case "$arg" in
		--runtime=*)
			runtime="${arg#*=}"
			;;
		--output=*)
			output="${arg#*=}"
			;;
		*)
			echo "usage"
			;;
	esac
done

if [ -z "$runtime" ]; then
	echo "Guessing runtime ..."

	case "$(uname -s)" in
		Linux)
			case "$(uname -m)" in
				x86_64)
					runtime="linux-x64"
					;;
			esac
			;;
	esac
fi

case "$runtime" in
	linux-x64)
		sofile="libsharpspades.so"
		;;
	*)
		fail "Unsupported runtime $runtime"
esac

echo Building for runtime "$runtime ..."
mkdir -p "$output/$runtime" && $CC $CFLAGS $SOURCES -shared $LDFLAGS -o "$output/$runtime/$sofile" && echo Done

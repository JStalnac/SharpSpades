#!/bin/sh
# SPDX-License-Identifier: MIT
depsjson="$(find "$1" -name '*.deps.json')"
patched="$(jq '.targets.".NETCoreApp,Version=v10.0"."SharpSpades.Native/1.0.0" += {"runtimeTargets": {"runtimes/linux-x64/native/libsharpspades.so": {"rid": "linux-x64", "assetType": "native"} }}' "$depsjson")"
echo -n "$patched" > "$depsjson"

#!/usr/bin/env bash

mono=

if [[ "$OS" != "Windows_NT" ]]; then
  mono=mono

  # http://fsharp.github.io/FAKE/watch.html
  export MONO_MANAGED_WATCHER=false
fi

$mono .paket/paket.exe restore || exit $?
./fake.sh run build.fsx "$@"

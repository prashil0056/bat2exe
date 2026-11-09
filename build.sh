#!/usr/bin/env bash
set -e

OUT="${1:-bat2exe.exe}"

if command -v mcs >/dev/null 2>&1; then
    COMPILER=mcs
elif command -v csc >/dev/null 2>&1; then
    COMPILER=csc
else
    echo "error: no C# compiler found. install Mono (brew install mono) or use Windows." >&2
    exit 1
fi

echo "using compiler: $COMPILER"

"$COMPILER" -nologo -target:exe -out:"$OUT" -r:System.dll -r:System.Core.dll Bat2Exe.cs

echo "built compiler: $OUT"
echo
echo "example:"
echo "  mono $OUT myscript.bat myscript.exe"
echo "  mono $OUT myscript.bat myscript.exe --hidden"

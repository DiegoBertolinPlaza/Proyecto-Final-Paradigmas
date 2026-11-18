#!/bin/sh
# Driver de merge de git para escenas, prefabs y assets de Unity (ver README, "Merge de escenas").
#
#   git config merge.unityyamlmerge.driver "sh tools/unityyamlmerge.sh %O %A %B %P"
#
# Por que no llamar a UnityYAMLMerge directamente:
#  - Decide el tipo de fichero por la extension, y git le pasa temporales sin extension
#    (.merge_file_XXXX). Sin saber que es una escena, se queda esperando a una herramienta de
#    merge externa que no tenemos y git se queda colgado.
#  - Con un conflicto de verdad (los dos cambiais la misma propiedad) tambien la busca y se cuelga.
#
# Este script copia los tres ficheros con su extension, le da un tiempo maximo y, si no
# consigue mezclar, deja las marcas de conflicto de siempre (<<<<<<< ======= >>>>>>>).
# Otra ruta de Unity: variable de entorno UNITY_YAML_MERGE.

base="$1"
ours="$2"
theirs="$3"
path="$4"

yaml="${UNITY_YAML_MERGE:-C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Data/Tools/UnityYAMLMerge.exe}"
ext="${path##*.}"
tmp=$(mktemp -d)

cp "$base" "$tmp/base.$ext"
cp "$theirs" "$tmp/theirs.$ext"
cp "$ours" "$tmp/ours.$ext"

if [ -f "$yaml" ] && timeout 30 "$yaml" merge -p "$tmp/base.$ext" "$tmp/theirs.$ext" "$tmp/ours.$ext" "$tmp/ours.$ext" </dev/null >/dev/null 2>&1; then
    cp "$tmp/ours.$ext" "$ours"
    status=0
else
    echo "UnityYAMLMerge no ha podido mezclar $path: quedan marcas de conflicto para resolverlo a mano." >&2
    git merge-file -L HEAD -L base -L entrante "$ours" "$base" "$theirs"
    status=1
fi

rm -rf "$tmp"
exit $status

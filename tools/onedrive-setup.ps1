# Prepara el proyecto para trabajar dentro de OneDrive (Windows).
#
#  1. Saca la carpeta Library/ (cache de Unity, 1-2 GB y miles de ficheros) fuera de OneDrive y
#     deja en su sitio una "union" (junction). Unity la usa igual, pero OneDrive no la sincroniza
#     y no bloquea ficheros mientras Unity compila.
#  2. Marca el resto del proyecto como "Mantener siempre en este dispositivo", para que OneDrive
#     nunca lo deje solo en la nube (Unity y git no pueden abrir ficheros que no estan en el disco).
#
# Uso (con Unity CERRADO), desde la raiz del repo:
#   powershell -ExecutionPolicy Bypass -File tools\onedrive-setup.ps1
#
# Se puede ejecutar las veces que haga falta: si ya esta hecho, no cambia nada.
# Nota: el texto va sin tildes a proposito; PowerShell 5.1 lee mal algunos caracteres UTF-8.

param(
    [string] $LibraryTarget = (Join-Path $env:LOCALAPPDATA 'MatriculaDeHonor\Library')
)

$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$library = Join-Path $project 'Library'

if (-not (Test-Path -LiteralPath (Join-Path $project 'ProjectSettings\ProjectVersion.txt'))) {
    throw "No parece la raiz de un proyecto Unity: $project"
}

$unityOpen = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" |
    Where-Object { $_.CommandLine -and $_.CommandLine.Contains($project) }
if ($unityOpen) {
    throw 'Cierra Unity antes de ejecutar este script.'
}

# ---- 1. Library fuera de OneDrive ----
$item = Get-Item -LiteralPath $library -Force -ErrorAction SilentlyContinue
if ($item -and $item.LinkType -eq 'Junction') {
    Write-Host "Library ya es una union -> $($item.Target)"
}
else {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $LibraryTarget) | Out-Null
    if ($item) {
        if (Test-Path -LiteralPath $LibraryTarget) {
            throw "Ya existe $LibraryTarget. Borralo o elige otra ruta con -LibraryTarget."
        }
        Write-Host "Moviendo Library a $LibraryTarget ..."
        Move-Item -LiteralPath $library -Destination $LibraryTarget
    }
    else {
        New-Item -ItemType Directory -Force -Path $LibraryTarget | Out-Null
    }

    cmd /c mklink /J "$library" "$LibraryTarget" | Out-Null
    $code = $LASTEXITCODE
    if ($code -ne 0 -or -not (Test-Path -LiteralPath $library)) {
        if ($item) {
            Write-Host 'No se pudo crear la union: devuelvo Library a su sitio.'
            Move-Item -LiteralPath $LibraryTarget -Destination $library
        }
        throw "mklink /J ha fallado (codigo $code). Ejecutalo desde una consola normal de PowerShell."
    }
    Write-Host "Union creada: Library -> $LibraryTarget"
}

# ---- 2. Todo lo demas, siempre en este dispositivo ----
attrib +P "$project" | Out-Null
Get-ChildItem -LiteralPath $project -Force | Where-Object { $_.Name -ne 'Library' } | ForEach-Object {
    if ($_.Attributes -band [IO.FileAttributes]::Hidden) { attrib +H +P $_.FullName | Out-Null }
    else { attrib +P $_.FullName | Out-Null }
    if ($_.PSIsContainer) { attrib +P "$($_.FullName)\*" /S /D | Out-Null }
}
Write-Host 'Proyecto marcado como "Mantener siempre en este dispositivo".'
Write-Host 'Listo. Si Unity vuelve a dar "Access ... is denied" en Library/Bee, pausa OneDrive mientras compila.'

# Lanza mlagents-learn con el entorno de Python del proyecto.
# (Sin tildes a proposito: PowerShell 5.1 lee mal el UTF-8 sin BOM.)
#
# El entorno virtual (PyTorch incluido, ~1 GB) se crea FUERA del repo, en
# %LOCALAPPDATA%\MatriculaDeHonor\ml-venv, para que OneDrive no intente sincronizarlo.
# La primera vez uv descarga Python 3.10.12 y las dependencias de uv.lock.
#
# Ejemplos (desde la carpeta ml-training):
#   .\train.ps1 --help
#   .\train.ps1 config\tribunal_smoke.yaml --env=<ruta>\BossTraining.exe --run-id=smoke --no-graphics
#   .\train.ps1 config\tribunal_boss.yaml --run-id=tribunal-01          (entrena contra el editor: dale a Play)

param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $MlagentsArgs
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command uv -ErrorAction SilentlyContinue)) {
    throw "No se encuentra 'uv'. Instalalo con: winget install --id=astral-sh.uv -e"
}

$env:UV_PROJECT_ENVIRONMENT = Join-Path $env:LOCALAPPDATA 'MatriculaDeHonor\ml-venv'

Push-Location $PSScriptRoot
try {
    uv run --locked mlagents-learn @MlagentsArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}

# Entrenamiento del jefe (ML-Agents)

El Tribunal (nivel 10) se entrena con PPO contra un jugador bot que usa el mismo `PlayerController`
que el jugador humano. Esta carpeta es la que pide la Entrega 2 junto al `.yaml`.

| Pieza | Dónde |
| --- | --- |
| Agente | `Assets/_Project/Scripts/ML/BossAgent.cs` (behavior `TribunalBoss`, 14 observaciones, 2 acciones continuas + 1 discreta de 3) |
| Escena de entrenamiento | `Assets/_Project/ML/Scenes/BossTraining.unity` (8 arenas en paralelo). Se rehace con *Tools/MDH/ML/Crear escena de entrenamiento* |
| Configuración | `config/tribunal_boss.yaml` (de verdad) y `config/tribunal_smoke.yaml` (prueba de humo) |
| Modelos entrenados | `Assets/_Project/ML/Models/*.onnx` |
| Resultados y checkpoints | `ml-training/results/` (no se sube) |

## Versiones

`com.unity.ml-agents` 4.2.0 ↔ `mlagents` 1.1.0, Python 3.10.12, PyTorch 2.1.1–2.8.0
(tabla de compatibilidad de la documentación del paquete). Todo fijado en `pyproject.toml` y `uv.lock`.

## Preparar el entorno (una vez)

```powershell
winget install --id=astral-sh.uv -e
cd ml-training
.\train.ps1 --help
```

La primera vez `uv` descarga Python 3.10.12 y las dependencias (~1 GB, sobre todo PyTorch) en
`%LOCALAPPDATA%\MatriculaDeHonor\ml-venv`, fuera del repo y de OneDrive.

## Entrenar

1. Compila el entorno: *Tools/MDH/ML/Build de entrenamiento (Windows)* en Unity, o en modo batch:

   ```powershell
   & "C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" -batchmode -quit -projectPath . `
     -executeMethod MatriculaDeHonor.ML.Editor.BossTrainingBuild.BuildFromCommandLine `
     -mdhBuildPath "$env:LOCALAPPDATA\MatriculaDeHonor\Builds\BossTraining"
   ```

2. Lanza el entrenamiento (desde `ml-training`):

   ```powershell
   .\train.ps1 config\tribunal_smoke.yaml --env="$env:LOCALAPPDATA\MatriculaDeHonor\Builds\BossTraining\BossTraining.exe" --run-id=smoke --no-graphics
   ```

   Sin `--env`, `mlagents-learn` espera a que le des a Play en el editor (útil para depurar, más lento).

3. Copia el modelo a Unity: `results\<run-id>\TribunalBoss.onnx` → `Assets/_Project/ML/Models/`
   y asígnalo en el campo *Model* del `Behavior Parameters` del jefe.

4. Curvas: `uv run --locked tensorboard --logdir results` (con `UV_PROJECT_ENVIRONMENT` apuntando al
   entorno, igual que en `train.ps1`).

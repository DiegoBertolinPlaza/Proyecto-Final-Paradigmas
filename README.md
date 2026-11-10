# Matrícula de Honor

Plataformas 3D para el proyecto final de Paradigmas de la Programación (ICAI, curso 2026-27).
Los protagonistas son estudiantes, los enemigos son profesores y los niveles recorren el campus.

## Abrir el proyecto

1. Instalar **Unity 6000.6.8f1** con Unity Hub (la versión que pide el enunciado).
2. Unity Hub → *Add project from disk* → seleccionar esta carpeta (la raíz del repo es el proyecto Unity).
3. La primera vez Unity regenera `Library/` (tarda unos minutos).

No clonéis el repo dentro de OneDrive, Dropbox o similares: Unity y Git dan problemas con la sincronización.

## Estructura

| Carpeta | Contenido |
| --- | --- |
| `Assets/_Project/Scripts/Runtime` | Código del juego (asmdef `MatriculaDeHonor.Runtime`) |
| `Assets/_Project/Scripts/ML` | Agente del jefe con ML-Agents (asmdef `MatriculaDeHonor.ML`) |
| `Assets/_Project/Tests/EditMode` | Tests unitarios (Window → General → Test Runner) |
| `ml-training/` | Configuración `.yaml` y entorno Python del entrenamiento (pendiente) |

## Flujo de trabajo con Git

- `main`: versión entregable, siempre jugable. Solo recibe PRs desde `develop` (Entrega 1) y `release/1.0` (Entrega 2).
- `develop`: integración. Cada tarea va en su rama (`feature/*`, `level/*`, `ml/*`, `fix/*`, `chore/*`, `docs/*`) que sale de `develop` y vuelve por PR revisada por la pareja.
- Antes de abrir una PR: compila, los tests EditMode pasan y la escena de prueba funciona.
- Cada escena tiene un único dueño; lo compartido va en prefabs.
- Nunca se suben `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/` ni `ml-training/results/` (ya están en `.gitignore`).

### Merge de escenas y prefabs

`.gitattributes` usa el driver `unityyamlmerge`. Cada uno debe registrarlo una vez en su máquina
(ajustando la ruta a su instalación de Unity):

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
```

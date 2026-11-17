# Matrícula de Honor

Plataformas 3D para el proyecto final de Paradigmas de la Programación (ICAI, curso 2026-27).
Los protagonistas son estudiantes, los enemigos son profesores y los niveles recorren el campus.

## Abrir el proyecto

1. Instalar **Unity 6000.6.8f1** con Unity Hub (la versión que pide el enunciado).
2. Unity Hub → *Add project from disk* → seleccionar esta carpeta (la raíz del repo es el proyecto Unity).
3. La primera vez Unity regenera `Library/` (tarda unos minutos).

Si el repo está dentro de **OneDrive**, antes de abrirlo ejecuta (con Unity cerrado):

```powershell
powershell -ExecutionPolicy Bypass -File tools\onedrive-setup.ps1
```

Saca `Library/` fuera de OneDrive con una unión de carpetas y marca el resto como «Mantener siempre
en este dispositivo», para que la sincronización no bloquee ficheros mientras Unity compila.

El diseño del juego está en [docs/GDD.md](docs/GDD.md) y la forma de trabajar (ramas, commits, PRs,
revisión) en [CONTRIBUTING.md](CONTRIBUTING.md).

## Estructura

| Carpeta | Contenido |
| --- | --- |
| `Assets/_Project/Scripts/Runtime` | Código del juego (asmdef `MatriculaDeHonor.Runtime`) |
| `Assets/_Project/Scripts/ML` | Agente del jefe con ML-Agents (asmdef `MatriculaDeHonor.ML`) |
| `Assets/_Project/Tests/EditMode` | Tests unitarios (Window → General → Test Runner) |
| `ml-training/` | Configuración `.yaml` y entorno Python del entrenamiento |
| `docs/` | GDD y plan semanal |
| `tools/` | Scripts de apoyo (por ejemplo `onedrive-setup.ps1`) |

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

## Problemas conocidos

### «Access to the path ...\Library\Bee\...\post-processed\*.dll is denied»

Unity no compila y el log muestra `UnauthorizedAccessException` al post-procesar una DLL de un paquete
(por ejemplo `Unity.Profiling.Core.dll`). No es un error de nuestro código: es la caché de compilación bloqueada.

1. Cierra Unity.
2. Borra la carpeta `Library/Bee` (es caché; Unity la regenera).
3. Vuelve a abrir el proyecto.

Si vuelve a pasar, suele ser el antivirus o OneDrive bloqueando la DLL recién escrita: ejecuta
`tools\onedrive-setup.ps1` si el repo está en OneDrive, o excluye la carpeta del proyecto del análisis
en tiempo real del antivirus.

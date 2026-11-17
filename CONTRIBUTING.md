# Cómo trabajamos

Reglas cortas para que dos personas toquen el mismo proyecto Unity sin pisarse.
El plan por semanas está en [docs/Plan_semanal_Matricula_de_Honor.pdf](docs/Plan_semanal_Matricula_de_Honor.pdf) y el diseño en [docs/GDD.md](docs/GDD.md).

## Preparar el entorno (una vez)

1. **Unity 6000.6.8f1** desde Unity Hub. Otra versión cambia `ProjectVersion.txt`: no lo subas.
2. Clona el repo y ábrelo con *Add project from disk*. Si lo tienes dentro de **OneDrive**, ejecuta
   antes `tools\onedrive-setup.ps1` (saca `Library/` de OneDrive y marca el resto como
   «Mantener siempre en este dispositivo»).
3. Registra el merge de escenas y prefabs (detalles en el README, «Merge de escenas y prefabs»):

   ```bash
   git config merge.unityyamlmerge.name "Unity SmartMerge"
   git config merge.unityyamlmerge.driver "sh tools/unityyamlmerge.sh %O %A %B %P"
   git config merge.unityyamlmerge.recursive binary
   ```

4. Solo si vas a entrenar el jefe: instala [uv](https://docs.astral.sh/uv/) (`winget install --id=astral-sh.uv -e`)
   y sigue [ml-training/README.md](ml-training/README.md).

## Ramas

- `main`: lo que se entrega. Solo recibe PRs de `develop` (Entrega 1) y `release/1.0` (Entrega 2).
- `develop`: integración. Siempre compila y los tests pasan.
- Cada tarea en su rama, que sale de `develop` y vuelve a `develop` por PR:

| Prefijo | Para | Ejemplo |
| --- | --- | --- |
| `feature/` | Una mecánica o sistema | `feature/player-controller` |
| `level/` | Un nivel | `level/01-entrada` |
| `ml/` | Entrenamientos y modelos | `ml/tribunal-v1` |
| `fix/` | Arreglar algo roto | `fix/compile-skeleton` |
| `chore/` | Configuración, paquetes, builds | `chore/ml-pipeline-check` |
| `docs/` | Documentación | `docs/gdd` |
| `tools/` | Herramientas de editor | `tools/scene-generator` |

## Commits

- Pequeños: uno por idea. Si el mensaje necesita un «y», probablemente son dos commits.
- Formato `tipo(ámbito): qué`, en español y en presente: `feat(player): salto de altura variable`.
- Tipos: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`, `style`.
- `push` al menos una vez al día, aunque la tarea no esté acabada.

## Pull requests

1. Abre la PR **en borrador** con el primer push: así el otro ve por dónde vas.
2. Cuando esté lista, quita el borrador y pide revisión. El otro revisa en **menos de 24 h**.
3. Antes de pedir revisión: compila sin errores ni avisos, los tests EditMode (y PlayMode si hay)
   pasan y la escena de prueba funciona.
4. Se fusiona con **Create a merge commit** y se borra la rama.

### Qué mira quien revisa

- ¿Hace lo que dice la tarea («Hecho cuando» del plan)?
- ¿Usa el patrón que toca sin forzarlo? (State, Observer, Strategy, Template Method, Decorator,
  Factory + Pool, Builder, Service Locator…). Si mete uno nuevo, que lo diga en la PR para el UML.
- ¿Hay tests para la lógica que no depende de la escena?
- ¿Toca escenas de otro? Cada escena tiene un único dueño; lo compartido va en prefabs.
- ¿Sube algo que no debe? (`Library/`, `Temp/`, builds, `ml-training/results/`).

## Tests

- Window → General → Test Runner, o en modo batch:
  `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults resultados.xml`
- La lógica pura (estados, eventos, estadísticas, observaciones del jefe) va en EditMode.
  Lo que necesita física o tiempo real (salto, colisiones) va en PlayMode.

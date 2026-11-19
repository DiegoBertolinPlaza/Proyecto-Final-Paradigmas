# Traspaso de ambientes a Claude

## Petición y decisiones del usuario

Crear ambientes reales en Unity, además de bocetos e ideas, y compartir el trabajo con Claude.
La escuela es ficticia y se llama IODP; Cum Laude sigue siendo el nombre del juego.
Las 21 fotos de `fotos ICAI/` inspiran la arquitectura y el mobiliario. No hay que copiar personas.
El usuario pide inventar biblioteca, ordenadores y escaleras. Capilla = arena del minijefe.
La escena del aula grande final es provisional hasta recibir esa referencia.

## Qué no debes rehacer

El trabajo está en `level/iodp-ambientes`, dentro del checkout principal. No cambiar su rama
mientras Codex valida con el editor. Las once escenas, los prefabs y las fichas ya están generados.
Lee `README.md` de esta carpeta y mira `index.html` para contrastar boceto y vista del motor.

## Archivos clave

- `Assets/_Project/IODP/Scenes/IODP_00_Campus.unity`: selector inicial.
- `Assets/_Project/IODP/Scenes/IODP_01_Entrada.unity` a `IODP_10_Tribunal.unity`: ambientes.
- `Assets/_Project/IODP/Prefabs`: 16 prefabs base. La subcarpeta Arquitectura incluye diez decorados horneados, la silla curvada, el motor docente y el puesto informático. La vitrina de la cafetería y la madera tienen atribuciones en CREDITOS_ARTE.md.
- `Assets/_Project/IODP/Data`: fichas de ambiente, niveles y configuración del jugador.
- `Assets/_Project/IODP/Resources/LevelCatalog.asset`: catálogo usado por el bootstrap existente.
- `Assets/_Project/Scripts/Editor/IODP`: Builder, Factory, generador y autoría local del editor.
- `Assets/_Project/IODP/Tests`: once tests de integridad de escenas, referencias y catálogo.
- `Logs/iodp`: logs, solicitudes locales de autoría e informes XML; no se sube a Git.

## Trabajo de jugador y combate

No se ha modificado tu trabajo en wt-player/wt-stomp ni se ha fusionado tu rama.
El parpadeo PlayerDamageFeedback se añadirá tras integrar tu rama, evitando referencias a clases
que no existen todavía en develop. Los ambientes usan el PlayerController actual sin forks.
También usan los Checkpoint, LevelGoal, CreditCollectible y HonorCollectible existentes.
F2 permite cambiar de ambiente a través de GameManager; las metas continúan al siguiente.

Para integrar enemigos, usar los nodos `[Reserva IA · no implementada en este ambiente]`.
Añadir NavMeshSurface y hornear después de cerrar el diseño de colisiones. Las arenas 05 y 10
necesitan conectar su jefe y bloquear la meta con lockedUntilBossDefeated. Actualmente se pueden
recorrer para comprobar su ambientación y no tienen combate activo. Charcos y servidores son
solo decorativos; no anunciar mecánicas de deslizamiento, electricidad o IA como terminadas.

## Integración de catálogo

El bootstrap detectará automáticamente el catálogo al cargar cualquier escena. No crear un
segundo asset Resources/LevelCatalog. Build Settings contiene primero el campus y los diez
ambientes; las escenas anteriores se conservan. La actualización de EditorBuildSettings.asset
forma parte de esta rama, pero el define automático SENTIS_ANALYTICS_ENABLED no.

## Comprobar antes de fusionar

Consultar `VALIDACION.md` y los informes de `Logs/iodp`. Revisar las capturas del motor, iniciar
partida desde el campus y desde una escena individual, probar F2, meta, pausa y reaparición.
No equivale a diez niveles equilibrados: falta la pasada de gameplay, enemigos y dificultad.
La PR debe ir a develop; el usuario revisa y abre/fusiona las PR, sin firmas ni coautor de IA.

# Instrucciones para Claude

Proyecto final de Paradigmas de la Programación (ICAI): **Cum Laude**, plataformas 3D en
Unity 6000.6.8f1 (el código aún usa el namespace `MatriculaDeHonor`). Diseño en `docs/GDD.md`; forma de trabajar en `CONTRIBUTING.md`.

## Reglas del equipo

1. Usar los patrones de diseño en la medida de lo posible, mientras no estropeen el código.
   Al añadir uno, decirlo en el commit o la PR (va al UML de la Entrega 1).
2. Seguir la planificación semanal, siempre con commits pequeños, push y ramas:
   cada tarea en su rama desde `develop`, commits `tipo(ámbito): qué` en español, PR a `develop`.

## Detalles prácticos

- La raíz del repo es la raíz del proyecto Unity. Código del juego en `Assets/_Project/Scripts`
  (asmdefs `MatriculaDeHonor.Runtime`, `.ML`, `.Editor`), tests en `Assets/_Project/Tests`.
- Antes de dar algo por hecho: compilar en modo batch sin errores ni avisos y pasar los tests
  EditMode (y PlayMode si existen) con Unity 6000.6.8f1.
- Unity en modo batch se lanza **fuera del sandbox**: dentro, `bee_backend` no puede arrancar sus
  procesos («Acceso denegado», GetLastError 5) y la compilación se queda colgada o falla.
- El repo puede estar dentro de OneDrive: `Library/` es una unión (junction) a una carpeta fuera
  de OneDrive (ver `tools/onedrive-setup.ps1`). No la borres ni la sustituyas por una carpeta.
- Al crear ficheros nuevos dentro de `Assets/`, subir también su `.meta`.
- No subir `ProjectVersion.txt` con otra versión de Unity ni cambios de ajustes que no vengan a
  cuento (por ejemplo el define `SENTIS_ANALYTICS_ENABLED`, que Unity añade y quita solo).
- Los scripts `.ps1` van sin tildes ni eñes: PowerShell 5.1 lee mal el UTF-8 sin BOM.
- Textos de la interfaz, comentarios y mensajes de commit en español.

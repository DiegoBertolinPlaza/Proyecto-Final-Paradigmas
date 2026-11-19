# IODP · Cuaderno de ambientes

## Decisiones del usuario

Escuela ficticia **IODP**, inspirada en las fotos de ICAI de `fotos ICAI/`. El juego mantiene
el título **Cum Laude**. No se reproduce el edificio a escala ni se modelan personas reales.
Biblioteca, sala de ordenadores y escaleras son inventadas con autorización expresa.
La capilla inspira el minijefe. El aula grande del jefe final se construye provisionalmente:
su fotografía aún no estaba en la carpeta al comenzar esta entrega.

## Decorados y mobiliario

Las once escenas se abren directamente en Unity. Cada nivel contiene un prefab de arquitectura que se puede expandir en la jerarquía. Los objetos se agrupan por elementos editables, como una vitrina, una crujía, un tramo de galería o una fila de butacas.

| Escena | Elementos del decorado |
|---|---|
| Entrada | Pórtico de sillería y ladrillo, patio con magnolios, pabellón de fondo y recepción |
| Aulas | Seminario con mesas orientadas por parejas, sillas de tubo y haya, pizarras de carril y paneles acústicos |
| Baños | Mamparas alicatadas, línea de espejos, dispensadores y sumideros longitudinales |
| Cafetería | Bancos tapizados, lámparas sobre las mesas, pase de cocina y dos vitrinas refrigeradas |
| La Revisión | Nave con costillas de madera, rosetón emplomado, alfombra y estrado |
| Biblioteca | Galerías altas con fondos, escalera, puestos de consulta y lámparas de lectura |
| Laboratorios | Nave de ladrillo con cerchas, bancos de motores y cigüeñal de exposición |
| Ordenadores | Islas de puestos, teclados, periféricos, cableado superior y armarios de servidores |
| Despachos | Galería porticada de tutorías, bancos de espera y jardineras |
| El Tribunal | Aula magna provisional con graderíos laterales, butacas, cortinajes y escenario |

La carpeta `Prefabs/Arquitectura` contiene diez decorados y tres modelos detallados de mobiliario. Los prefabs originales del juego conservan sus referencias. El directorio de inicio usa los diez destinos existentes. La galería incluye un render del motor y el boceto de intención de cada zona. [Galería](index.html) · [Créditos de los recursos](CREDITOS_ARTE.md)

## Abrirlo en Unity

1. Unity **6000.6.8f1**, carpeta `C:\Dev\ProyectoFinal`, rama `level/iodp-ambientes`.
2. Si las escenas aún no existen: **Tools → IODP → Crear los 10 ambientes (sin sobrescribir)**.
3. Abrir `Assets/_Project/IODP/Scenes/IODP_00_Campus.unity` y pulsar Play.
4. Elegir una zona. WASD/flechas, Espacio, Shift, Ctrl/C; Esc pausa. **F2** abre el selector.
5. Seguir ECTS ámbar hasta la puerta turquesa para continuar. El sello turquesa es el checkpoint.

El generador conserva escenas y prefabs existentes para no destruir cambios manuales. No hace
falta regenerar para jugar: las escenas guardadas contienen la geometría. El catálogo Resources
vive dentro de IODP; al integrar con un catálogo futuro hay que unificarlo, nunca duplicarlo.
El generador añade las once escenas a Build Settings y conserva las escenas previas.

## Lenguaje visual común

- Crema `#EEE6D6`, verde `#667D65`, madera `#B1814F`, piedra `#A9A69E`.
- Ámbar `#E9AE49`: pistas de recorrido, créditos y cantos de plataformas.
- Turquesa `#4FA99E`: checkpoint, meta y diplomas.
- Materiales URP con texturas locales de madera, terrazo, gres, caliza, ladrillo y tejido. El modelo de vitrina y la madera incluyen créditos en `CREDITOS_ARTE.md`.
- Los techos tienen una cara visible desde el interior. La vista elevada permite editar y seguir el recorrido.
- Escala en metros: estudiante de 1,8 m; sala base de 28 × 56 m; avance hacia +Z.
- La entrada, los muebles y los colores son reinterpretaciones reconocibles, no fotogrametría.

## Diez ambientes y siguientes mecánicas

| # | Ambiente | Referencia / composición | Siguiente trabajo de juego |
|---|---|---|---|
| 01 | Entrada | Arcos de piedra y ladrillo, rótulo IODP, recepción, tornos abiertos, bancos y descansillo. Fotos 2, 3, 7, 15. | Tutorial y bedel. |
| 02 | Aulas | Pupitres con faldones verdes, pizarra, tarima y radiador. Fotos 1 y 6. | Profe de Cálculo; tizas y coberturas. |
| 03 | Baños | Lavabos, espejos azules, cabinas de madera, suelo cálido. Fotos 13, 14, 21. | Deslizamiento y empuje de secadores. |
| 04 | Cafetería | Mesas, café, bandejas y pilares con banda de madera. Fotos 4, 5, 8. | Café y plataformas móviles. |
| 05 | La Revisión | Bancos a los lados, atril, estrado y luz cálida. Capilla, foto 17. | Minijefe programado. |
| 06 | Biblioteca | Librerías coloreadas, pasillos, mesas y altillo. Inventada. | Bibliotecaria y sigilo por ruido. |
| 07 | Laboratorios | Bloques motor, bancadas y perímetros amarillos. Fotos 9, 10, 11, 20. | Riesgos mecánicos/químicos y Bata. |
| 08 | Ordenadores | Filas de monitores y servidores. Inventada. | Profe de Programación; fallos y láseres. |
| 09 | Despachos | Puertas repetidas, marcos, ventanas altas y plantas. Fotos 16, 18. | Mezcla de enemigos y atajos. |
| 10 | El Tribunal | Aula magna con gradas, pantalla y tarima. Provisional hasta recibir foto. | Jefe ML-Agents y cierre del juego. |

La numeración de fotos corresponde a la hoja `referencias_revision/contacto.jpg` fuera del repo;
el orden es el nombre de archivo ordenado alfabéticamente. Los originales no se modifican ni se
incluyen en materiales del juego. La foto 12 está obstruida y no se usa para diseñar.

## Para Claude y Codex

- Codex se ocupa de estos ambientes: `Assets/_Project/IODP`, `Scripts/Editor/IODP`, `docs/IODP`.
- Claude mantiene el trabajo del jugador en `C:\Dev\wt-player`, rama `feature/player-stomp-damage`.
- No se han modificado PlayerController, PlayerStates ni Tests/PlayMode para crear ambientes.
- Para añadir parpadeo después de integrar la rama del jugador, añadir PlayerDamageFeedback al
  estudiante de cada escena o al Builder. Ahora no se referencia una clase ausente en develop.
- La IA debe colocarse en los nodos `[Reserva IA · no implementada en este ambiente]` y conectar
  las metas de los jefes a su evento BossDefeated al integrar el combate.
- Primer paso de integración: PR de ambientes hacia develop; no fusionar ni cambiar ramas
  mientras Unity esté escribiendo escenas. Consultar el estado actual en handoff.md.

## Patrones para el UML

**Builder** (`CampusGeometry`, director `CampusSceneGenerator`), **Factory** de prefabs
(`CampusPropFactory`), datos en **ScriptableObject** (`CampusEnvironmentDefinition`). Se reutilizan
el State de GameManager, los eventos Observer y los coleccionables del código existente.

## Validación

Las comprobaciones y sus resultados concretos se anotan al terminar la generación, en
`VALIDACION.md`. No se debe confundir un boceto SVG con una captura del motor: la galería
etiqueta por separado «boceto» y «Unity».

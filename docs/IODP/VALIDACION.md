# Validación de los ambientes IODP

Revisión del 19 de noviembre de 2026 con Unity 6000.6.8f1, en una copia independiente del proyecto de la rama `level/iodp-ambientes`.

| Comprobación | Resultado |
|---|---|
| Suite EditMode del proyecto | 54 de 54 aprobadas |
| Suite PlayMode del proyecto | 9 de 9 aprobadas |
| Comprobación adicional del directorio | En Play se abrió el campus, se resolvieron sus diez destinos y la acción de la fila Cafetería cargó IODP_04_Cafeteria |
| Revisión gráfica | Diez PNG de Unity a 1280 × 800. Se revisaron el color del pórtico, los rótulos, la escala del mobiliario y los encuadres interiores |
| Entrega | Se contrastó por SHA-256 cada archivo copiado desde Unity. Todas las escenas y assets tienen su .meta |

Los once tests de integridad cubren las diez escenas, las referencias serializadas, la cámara, los materiales, el suelo, los coleccionables, el catálogo y las escenas incluidas en la build. Los informes locales se guardan en `Logs/iodp/EditMode-entrega.xml`, `PlayMode-entrega.xml` y `directorio.xml`. La comprobación adicional del directorio invocó la acción usada por el botón.

La revisión se centró en la base visual. El ajuste manual de saltos y dificultad queda para la fase de juego. El diseño del aula magna es provisional hasta recibir la foto. [Galería de capturas](index.html)

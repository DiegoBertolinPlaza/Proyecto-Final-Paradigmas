# Matrícula de Honor — GDD de una página

> Documento vivo. Lo marcado con **[decidir]** está propuesto pero falta que lo cerremos los dos.
> Si cambia algo del diseño, se cambia aquí en una PR `docs/*`.

| | |
| --- | --- |
| **Género** | Plataformas 3D por niveles, cámara semifija |
| **Motor** | Unity 6000.6.8f1 · URP · Input System · ML-Agents 4.2 |
| **Plataforma** | PC (Windows), teclado o mando · 1 jugador |
| **Entregas** | E1 03/12/2026: descripción + UML · E2 10/01/2027: build + proyecto + `.yaml` |

## Idea

Eres un estudiante del ICAI que quiere sacar Matrícula de Honor. Cruzas diez zonas del campus
recogiendo créditos ECTS y Matrículas de Honor mientras los profesores intentan que suspendas.
Los profesores son **arquetipos inventados**, no personas reales.

## Bucle de juego (lo pide el enunciado)

Menú → **Jugar** → nivel → llegar a la meta (**victoria**) → siguiente nivel … → nivel 10 → fin.
Si pierdes todas las **convocatorias** (vidas, empiezas con 4) → **suspenso** (derrota) →
**volver a jugar** desde el menú, con la partida reiniciada. Cada 100 ECTS dan una convocatoria extra.

## Controles

| Acción | Teclado | Mando |
| --- | --- | --- |
| Moverse | WASD / flechas | Stick izquierdo |
| Correr | Shift izquierdo | X / Cuadrado |
| Saltar (más alto si lo mantienes) | Espacio | A / Cruz |
| Culetazo (en el aire) | Ctrl izquierdo / C | Gatillos |
| Pausa | Esc / P | Start |
| Depuración de la IA | F1 | — |

## El estudiante

Correr y saltar con *coyote time* y *buffer* de salto, pisotón con rebote (más alto si mantienes
saltar), culetazo, daño con retroceso e invulnerabilidad breve. Correr hace ruido y los profesores
lo oyen. Power-ups como datos (patrón Decorator): **Café** (más velocidad), **Sudadera** (menos
ruido), **Bata** (inmune a charcos químicos), **Chuleta** (un escudo).

## Profesores (IA capa 1: máquina de estados + visión y oído)

| Profesor | Qué hace | Desde |
| --- | --- | --- |
| Bedel | Patrulla y te pilla por contacto | Nivel 1 |
| Profe de Cálculo | Lanza tizas apuntando a donde vas a estar (apuntado predictivo) | Nivel 2 |
| Bibliotecaria | Poca vista y oído finísimo; grita «¡SILENCIO!» y avisa a los demás | Nivel 6 |
| Profe de Química | **[decidir]** deja charcos que hacen daño | Nivel 7 |
| Profe de Programación | **[decidir]** | Nivel 8 |

Todos comparten `EnemyBase` y solo cambian sus ganchos (Template Method); el ataque es una
estrategia intercambiable (Strategy).

## Jefes (IA capa 2)

- **Nivel 5 · La Revisión**: mini-jefe con cerebro programado (`ScriptedBossBrain`).
- **Nivel 10 · El Tribunal**: jefe final entrenado con **ML-Agents (PPO)** contra un jugador bot que
  usa el mismo controlador que tú. Ve 14 valores (distancia, altura, tu velocidad, vidas,
  enfriamientos, posición en la arena) y decide moverse, lanzar o embestir. Se le gana con
  pisotones. Si no hay modelo entrenado usa el cerebro programado, así el juego nunca se rompe.
  Los checkpoints intermedios del entrenamiento sirven como dificultades **[decidir]**.

## Niveles

| # | Zona | Lo nuevo |
| --- | --- | --- |
| 1 | Entrada | Correr, saltar, Bedel |
| 2 | Aulas | Tizas del profe de Cálculo |
| 3 | Baños | **[decidir]** suelo resbaladizo |
| 4 | Cafetería | Power-up Café |
| 5 | La Revisión | Mini-jefe |
| 6 | Biblioteca | Sigilo: la Bibliotecaria te oye |
| 7 | Laboratorios | Charcos químicos y Bata |
| 8 | Ordenadores | Profe de Programación |
| 9 | Despachos | Mezcla de todo, más difícil |
| 10 | El Tribunal | Jefe final con ML-Agents |

## Estilo

Formas simples y colores planos (prototipo con cubos y cápsulas). Para el arte final, assets
gratuitos con licencia libre **[decidir: pack concreto]**. Prioridad del enunciado: código y
patrones por encima del arte.

## Reparto

| | Se encarga de | Quién |
| --- | --- | --- |
| **A** | Jugador, cámara, niveles, UI y presentación | **[decidir]** |
| **B** | IA de profesores, jefes, ML-Agents y bucle de juego | **[decidir]** |

Cada PR la revisa el otro. El detalle por semanas está en [Plan_semanal_Matricula_de_Honor.pdf](Plan_semanal_Matricula_de_Honor.pdf).

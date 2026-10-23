# Reglamento del juego de cartas — v1.0

> Transcripción del documento oficial `Docs/Juego_Cartas_Reglamento_Definitivo_v1.0.docx`.
> Si hay diferencias, manda el Word. Cualquier cambio de reglas se hace primero en el Word y luego se actualiza aquí y en `CLAUDE.md` §6.

Juego competitivo de rondas rápidas, presión numérica y manipulación táctica de la mesa.

| Mazo | Mano | Valores | Comodines |
|---|---|---|---|
| 56 cartas | 4 cartas | 1 a 10 | 16 (4 tipos × 4) |

## 1. Objetivo de la partida
La partida se juega en varias rondas. En cada ronda, los jugadores intentan reducir su mano mientras modifican el valor, la comparación y el orden de juego para dificultar las jugadas de sus rivales.

**Condición de victoria final:** cuando ya no se puede iniciar otra ronda, gana el jugador que haya acumulado más puntos durante la partida.

## 2. Composición del mazo

| Tipo | Copias | Total |
|---|---|---|
| Números 1-10 | 4 copias de cada número | 40 |
| Cambio de sentido | 4 | 4 |
| Invertir comparación | 4 | 4 |
| Salto | 4 | 4 |
| Fijar valor | 4 | 4 |
| **Total** | | **56** |

## 3. Preparación de una ronda
1. Baraja únicamente las cartas que todavía permanecen disponibles en la partida.
2. Reparte exactamente 4 cartas a cada jugador.
3. En la primera ronda, el jugador inicial se elige al azar. En cada ronda posterior, el jugador inicial rota obligatoriamente un asiento en sentido horario.
4. Revela una carta numérica para establecer el valor inicial de la mesa. Si aparece un comodín, se aparta temporalmente y se continúa hasta obtener una carta numérica; los comodines apartados regresan a la reserva disponible antes de iniciar la ronda.
5. La regla de comparación comienza en modo ascendente: se debe jugar un número igual o mayor que el valor de referencia.
6. El sentido inicial de juego es horario.

**Rotación obligatoria:** la elección del jugador inicial de una ronda no depende del ganador anterior, del jugador bloqueado ni del sentido con el que terminó la ronda.

## 4. Estado público de la mesa
En todo momento existen tres variables públicas que determinan qué jugadas son válidas:
- **Valor de referencia:** un número entre 1 y 10.
- **Regla de comparación:** ascendente (>=) o descendente (<=).
- **Sentido de juego:** horario o antihorario.

Ejemplo: con valor 6 y regla >= son válidos 6, 7, 8, 9 y 10, además de cualquier comodín. Con valor 6 y regla <= son válidos 1, 2, 3, 4, 5 y 6, además de cualquier comodín.

## 5. Turno de un jugador
1. El jugador comprueba si dispone de al menos una jugada legal.
2. Si no dispone de ninguna jugada legal, la ronda termina inmediatamente por bloqueo.
3. Si juega una carta numérica válida, ese número pasa a ser el nuevo valor de referencia.
4. Si tiene varias copias del mismo número y ese número es válido, puede jugar varias copias iguales simultáneamente como una única jugada.
5. También puede jugar un comodín; los comodines no están sujetos a la comparación numérica.
6. Se aplica el efecto de la carta y se determina el siguiente jugador.
7. Si después de una jugada legal el jugador queda con 0 cartas, la ronda termina inmediatamente con victoria por vaciado.

**No se puede pasar:** si el jugador dispone de una jugada legal, debe jugar. Si no dispone de ninguna, se produce un bloqueo y termina la ronda.

## 6. Regla de las cartas numéricas
Con comparación ascendente (>=), una carta numérica es válida cuando su valor es igual o mayor que el valor de referencia. Con comparación descendente (<=), es válida cuando su valor es igual o menor.

**Cartas iguales:** se pueden jugar varias copias del mismo número en un único turno. No se pueden mezclar números diferentes en la misma jugada.

## 7. Comodines

| Comodín | Efecto | Observación |
|---|---|---|
| Cambio de sentido | Invierte horario ↔ antihorario. | No modifica el valor ni la comparación. |
| Invertir comparación | Cambia >= por <= o <= por >=. | El valor de referencia permanece igual. |
| Salto | El siguiente jugador pierde su turno. | En 2 jugadores, quien lo juega actúa inmediatamente otra vez. |
| Fijar valor | El jugador elige cualquier valor entero entre 1 y 10 como nueva referencia. | No modifica la comparación ni el sentido. |

Todos los comodines pueden jugarse independientemente del valor numérico de la mesa. Salvo Fijar valor, ninguno sustituye ni modifica el valor de referencia.

**Partida de 2 jugadores:** Cambio de sentido invierte formalmente la dirección, pero con solo dos jugadores no cambia quién será el siguiente jugador. La regla se mantiene tal cual.

## 8. Cómo termina una ronda

### 8.1. Victoria por vaciado — 3 puntos
Si un jugador realiza una jugada legal y, como resultado, se queda con 0 cartas, gana la ronda inmediatamente. No se comparan las manos de los demás jugadores.

### 8.2. Victoria por bloqueo — 1 punto
Si un jugador comienza su turno sin ninguna jugada legal, la ronda termina inmediatamente. Entonces se comparan las manos de todos los jugadores:
1. Gana quien tenga menor cantidad de cartas restantes.
2. Si hay empate en cantidad, gana quien tenga la menor suma total de valores en su mano.
3. Para el desempate, cada comodín restante cuenta como 10 puntos.
4. Si también hay empate en la suma, se considera un empate real entre esos jugadores.

En caso de empate real por bloqueo, cada jugador empatado recibe 1 punto.

## 9. Persistencia del mazo
Al terminar cada ronda, todas las cartas que participaron en ella salen permanentemente de la partida: las cartas jugadas, las que quedaron en las manos y la carta numérica inicial de la mesa. No regresan al mazo durante esa partida; la siguiente ronda usa solo las cartas no usadas.

**Consumo por ronda:** con *m* jugadores, una ronda consume exactamente **4m + 1** cartas.

## 10. Fin de la partida y ganador
La partida termina cuando no quedan suficientes cartas para repartir 4 cartas a cada jugador y colocar una carta numérica inicial. Gana quien tenga más puntos acumulados. Si dos o más jugadores empatan en puntos, la partida termina en empate entre ellos.

## 11. Ejemplo completo
Mesa inicial: valor 4, comparación >=, sentido horario. Jugadores A, B, C y D.
1. A juega dos cartas 6 simultáneamente. Su mano se reduce en dos cartas y la referencia pasa a 6.
2. B juega 10. La referencia pasa a 10.
3. C juega Invertir comparación. La mesa sigue en 10, pero ahora la comparación es <=.
4. D juega 3. La referencia pasa a 3.
5. A juega Salto. B pierde su turno y el siguiente jugador es C.
6. C no tiene número <= 3 ni comodín. La ronda termina por bloqueo.
7. Se comparan las manos. El jugador con menos cartas gana la ronda y obtiene 1 punto.

Si en cualquiera de sus turnos un jugador hubiera usado legalmente su última carta, la ronda habría terminado en ese momento y ese jugador habría obtenido 3 puntos.

## 12. Resumen rápido

| Concepto | Regla |
|---|---|
| Mazo | 56 cartas: números 1-10 ×4 + 4 tipos de comodín ×4 |
| Mano inicial | 4 cartas por jugador |
| Regla inicial | Jugar un número igual o mayor; sentido horario |
| Jugadas | Una carta numérica válida, varias copias iguales o un comodín |
| Bloqueo | Sin jugada legal → termina la ronda → menor mano gana → 1 punto |
| Vaciado | Usar legalmente la última carta → victoria inmediata → 3 puntos |
| Entre rondas | Todas las cartas de la ronda se retiran permanentemente |
| Jugador inicial | Rota un asiento en sentido horario cada ronda |
| Final | Cuando no alcanza el mazo para otra ronda, gana quien tenga más puntos |

## Notas para la implementación
- En el reglamento **no se roba** del mazo durante la ronda: solo se reparte al inicio. El "robar del mazo" del prototipo actual es solo para probar las animaciones.
- El juego en Unity es de **2 jugadores** (jugador contra orco), así que Salto = volver a jugar y Cambio de sentido no cambia el turno.
- Fijar valor requiere una interfaz para elegir del 1 al 10.

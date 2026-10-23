# CLAUDE.md — Videojuego 3D "Diseño Creativo"

> Documento vivo. **Léelo antes de trabajar en el proyecto y actualízalo cuando cambie algo importante**: reglas del juego acordadas, scripts nuevos, decisiones técnicas o problemas conocidos.
> Idioma del proyecto: **español** (conversación, comentarios en el código, commits y documentación).

---

## 1. Contexto

- Proyecto para la materia universitaria **Diseño Creativo**. El objetivo es entregar un **videojuego en 3D** (requisito del profesor).
- El usuario no es programador experto: explicarle los pasos en Unity de forma concreta (qué menú, qué botón) y sin jerga innecesaria.
- **Concepto: juego de cartas en 3D.** El jugador recorre un mini calabozo y se sienta a una mesa a jugar cartas contra personajes (por ahora, un orco). Las reglas están en el **Reglamento v1.0** (`Docs/Reglamento.md`, resumen en §6).
- **Estado actual:** prototipo jugable: moverse por el calabozo, recoger objetos, abrir cofres y la reja, y sentarse a la mesa frente al orco. Al sentarse se reparten cartas **genéricas** (por ahora solo números del 1 al 10) y se pueden poner, agarrar y recoger con animaciones. **Las reglas todavía no se aplican** (no hay turnos, validación de jugadas ni IA).

---

## 2. Herramientas y versiones

| Herramienta | Versión / nota |
|---|---|
| Unity | **6000.6.6f1** (Unity 6.6). El proyecto se migró desde 6000.5.9f1; ya no usar esa versión |
| Render pipeline | **URP** (plantilla Universal 3D). Assets de render en `Assets/Settings/` (`PC_RPAsset`, `Mobile_RPAsset`) |
| Graphics API | **DirectX 11** a propósito, por compatibilidad con PCs de la escuela. No activar Auto Graphics API (DX12) |
| Input | **Input System nuevo** únicamente (`activeInputHandler: 1`). Acciones del proyecto en `Assets/InputSystem_Actions.inputactions` |
| MCP | **MCP for Unity** (CoplayDev) `v10.0.0`, transporte HTTP local `http://127.0.0.1:8080`. Hay actualización a v10.3.0 pendiente; no actualizar sin comprobar antes |
| Control de versiones | Git + Git LFS. Repo en la raíz `Diseño Creativo/`, rama `main`. Remoto `origin`: https://github.com/CristianRei/Video-Juego-Cartas-Dise-o-Creativo (aquí se trabaja el proyecto). Los modelos, imágenes y audio van por LFS (cuota gratis de GitHub: 1 GB) |
| Blender | **No se usa por ahora.** Decisión del usuario: los modelos 3D se descargan de internet |

### Usar el MCP de Unity
1. Unity debe estar **abierto** con el proyecto `JuegoDisenoCreativo`.
2. En Unity: **Window → MCP for Unity → Start Server**, hasta que el indicador quede en **verde**.
3. Hacer esto **antes** de abrir la sesión de Claude. Si el servidor se apaga (por ejemplo, porque el PC entró en reposo), las herramientas `mcp__UnityMCP__*` responden `no_unity_session` y hay que pedirle al usuario que lo vuelva a encender.
- `execute_code` compila con CodeDom (**C# 6**): no usar LINQ sin `using`, ni tuplas, ni pattern matching moderno dentro de ese código. Los scripts `.cs` del proyecto sí pueden usar C# moderno.
- Después de crear o editar scripts: `refresh_unity` → `read_console` para ver errores de compilación.
- Si las herramientas `mcp__UnityMCP__*` no están en la sesión pero el servidor responde (`http://127.0.0.1:8080/health`), se puede llamar por HTTP con JSON-RPC: `initialize` → cabecera `mcp-session-id` → `notifications/initialized` → `tools/call`. La respuesta llega como SSE (`data: {...}`).
- Unity **no puede ejecutarse en batchmode** (`-batchmode`) en este PC: falla la licencia. Todo se hace con el editor abierto y el MCP.
- Después de recompilar, Unity a veces no se reconecta al MCP si su ventana está en segundo plano. Hay que pedirle al usuario que haga clic en Unity. Se puede saber si sigue compilando mirando el título de la ventana (proceso `Unity`, contiene "Compiling" o "busy").
- **Probar en Play desde el MCP:** con Unity sin foco, el modo Play casi no avanza cuadros (`Time.frameCount` se queda quieto). Se avanza a mano con `EditorApplication.isPaused = true` + `EditorApplication.Step()` en un bucle (síncrono) y luego se saca la captura. Al detener Play se quita la pausa.

---

## 3. Estructura del repositorio

```
Diseño Creativo/                    ← raíz del repo git
├── CLAUDE.md                       ← este archivo
├── README.md
├── .gitattributes                  ← reglas de LFS (fbx, png, wav…) y merge YAML de Unity
├── .gitignore                      ← ignora /kenney_*/ (paquetes descargados sin procesar)
├── kenney_*/                       ← descargas originales del usuario (NO versionadas; se copian a Assets cuando se usan)
├── Blender/  (scripts/, export/)   ← reservado, sin uso por ahora
├── Docs/                           ← documentos de diseño: Reglamento v1.0 (.docx oficial + Reglamento.md)
└── JuegoDisenoCreativo/            ← proyecto Unity
    ├── .gitignore                  ← Library/, Temp/, Logs/, Assets/Screenshots/…
    ├── Packages/manifest.json
    ├── ProjectSettings/
    └── Assets/
        ├── InputSystem_Actions.inputactions
        ├── Settings/               ← assets de URP (de la plantilla)
        └── _Game/                  ← TODO lo nuestro va aquí
            ├── Scenes/             Main.unity (vacía, base), MiniDungeon.unity (prototipo)
            ├── Scripts/
            │   ├── Player/         PlayerController.cs, CameraFollow.cs
            │   ├── Gameplay/       IInteractable.cs, Openable.cs, Seat.cs, Collectible.cs
            │   ├── Cards/          CardData.cs, CardView.cs, CardTable.cs
            │   ├── UI/             GameHUD.cs, InteractionPromptUI.cs
            │   ├── Managers/       (vacío)
            │   └── Editor/         MiniDungeonBuilder.cs
            ├── Models/
            │   ├── MiniDungeon/                 ← Kenney Mini Dungeon (FBX + Textures/colormap.png)  ★ estilo principal
            │   └── Characters/KenneyProtagonists ← Kenney Animated Characters (no se usa ahora; otro estilo)
            ├── Animations/         Player, Orc, Chest, Gate, Trap (.controller, generados por el builder)
            ├── Materials/ Textures/ Prefabs/ Audio/{Music,SFX}/ UI/   (vacíos, con .gitkeep)
```

**Convenciones**
- Todo el contenido propio va en `Assets/_Game/`. No tocar `Assets/Settings/` salvo para ajustar el render.
- Cada script en la subcarpeta de su sistema. Clases sin namespace (como las actuales), comentarios en español y breves.
- Los paquetes descargados se dejan en la raíz (`kenney_*`, ignorados) y **solo se copian a `Assets/_Game/Models/<Paquete>/`** los formatos que se usan (FBX + texturas + License.txt).
- Los assets deben ser de licencia libre (Kenney = CC0). Anotar la fuente en §8.
- Commits en español, terminados con la línea `Co-Authored-By` que indique el sistema.

---

## 4. Arte y estilo

- **Estilo visual elegido: "mini" low-poly de Kenney Mini Dungeon.** Personajes cabezones de unos 0.76 u de alto, todo con una sola textura atlas (`colormap.png`, material URP Lit).
- **Escala:** 1 unidad = 1 baldosa. Pisos 1×1, muros como bloques de 1×1.1×1 que ocupan una celda completa, pivote en la base y centrados.
- Nuevos assets deben respetar este estilo (por ejemplo, otros packs "mini" de Kenney o Quaternius).
- El pack *Animated Characters Protagonists* (personaje realista de 1.8 u) se descartó por no encajar con el estilo; queda en el proyecto por si acaso.

### Modelos de Mini Dungeon
- **Animados** (rig Generic, avatar *Create From This Model*; los configura el builder):
  - `character-human`, `character-orc` (32 clips cada uno): `idle`, `walk`, `sprint`, `jump`, `fall`, `crouch`, `die`, `sit`, `pick-up`, `attack-melee-left/right`, `attack-kick-left/right`, `interact-left/right`, `emote-yes/no`, `holding-left/right/both` (+`-shoot`), `drive`, `static`, `wheelchair-*`.
  - `chest`, `gate`: `open`, `close`, `open-close`.
  - `trap`: `show`, `hide`, `show-hide`.
- **Estáticos:** floor, floor-detail, wall, wall-half, wall-narrow, wall-opening, column, stairs, dirt, rocks, stones, barrel, pot, table, chair, banner (se cuelga de la cara sur de un bloque de muro cuando rot=0), wood-structure, wood-support, coin, key, potion, weapon-sword, weapon-spear, shield-round, shield-rectangle.

---

## 5. Código actual (qué hace cada cosa)

### `PlayerController.cs` (Player/)
- Requiere `CharacterController`. Movimiento relativo a la cámara, rotación suave hacia la dirección de avance.
- Lee acciones de `InputSystem.actions` (acciones a nivel de proyecto): `Player/Move`, `Jump`, `Sprint`, `Attack`, `Interact`.
- Velocidades: caminar 1.6, correr 3.2, salto 0.6 u, gravedad −15.
- Atacar e interactuar bloquean el movimiento durante `attackDuration` (0.5 s).
- Interactuar: en cada cuadro, `FindInteractable()` hace un `OverlapSphereNonAlloc` frente al jugador y guarda en `FocusedInteractable` el `IInteractable` disponible (`CanInteract`) más cercano; la UI de ayuda lo usa. E llama a `Interact` sobre ese objeto.
- Sentarse: `SitOn(seat)` desactiva el `CharacterController`, coloca al jugador en `seat.SitPosition` y activa `Sit`. Mientras está sentado (`IsSitting`, `CurrentSeat`), E llama de nuevo al asiento, que ejecuta `StandUp()` (lo deja detrás de la silla).
- Parámetros del Animator: `Speed` (float), `Grounded` (bool), `Sit` (bool), `Jump`, `Attack`, `Interact` (triggers).

### `IInteractable.cs` (Gameplay/)
- Interfaz para todo lo que se usa con E: `CanInteract(player)`, `Interact(player)`, `GetPrompt(player)` (texto de la ayuda, por ejemplo "Sentarse" o "Abrir") y `PromptPosition` (punto del mundo donde se dibuja la ayuda). La implementan `Openable` y `Seat`. Las cosas nuevas que se puedan usar con E deben implementarla.

### `Seat.cs` (Gameplay/)
- Va en la raíz de una silla, cuyo frente es el eje Z local. `sitOffset` = (0, 0.30, 0.13): la animación `sit` de Kenney es para sentarse **en el suelo** (cadera unos 0.11 bajo el origen) y el asiento de `chair` está a 0.19 de alto. Z = 0.13 lo adelanta para que el respaldo quede detrás del personaje y no se vea "fusionado" con él (con 0.03 se veía así).
- `seatedAtStart`: Animator de un personaje (no jugador) que empieza sentado. En `Start` lo coloca y activa `Sit`. Una silla ocupada no deja sentarse al jugador.
- Eventos `PlayerSat` y `PlayerStoodUp`: los usa `CardTable` para repartir y recoger.

### `CardData.cs` (Cards/)
- `CardKind` (Number, Reverse, InvertComparison, Skip, SetValue) y la estructura `CardData` (tipo + valor 1-10, `IsWildcard`, `TieBreakValue` donde el comodín vale 10).
- `CreateFullDeck()` arma el mazo de 56 del reglamento. `CreateNumberDeck()` arma solo números (40). `Shuffle()` usa Fisher-Yates.

### `CardView.cs` (Cards/)
- Carta 3D **genérica y provisional** construida por código: un cubo de 0.09 × 0.13 × 0.002 (que también es su collider para el clic), un reverso rojo con rombo dorado, y el número con **TextMeshPro** al centro y en las esquinas (el 6 y el 9 van subrayados). Los comodines tienen cara de color y nombre. La cara frontal mira hacia −Z local.
- Movimiento: `FlyTo(pos, rot, duración, arco)` vuela en arco con suavizado. `SetTarget` cambia el destino sin cortar el vuelo. Fuera de vuelo sigue a su destino con suavizado exponencial.
- Materiales URP Lit creados en tiempo de ejecución y cacheados por color.

### `CardTable.cs` (Cards/)
- Va en la mesa (`table`). **En el centro: el mazo (izquierda, visto por el jugador) y el montón de cartas jugadas (derecha)**, separados ±`centerSpacing` (0.08). La superficie de la mesa está a `tableTopHeight` = **0.316** (medido con raycast; las cotas del modelo dicen 0.31, y con eso las cartas quedaban ocultas).
- `numbersOnly = true`: **por ahora solo cartas numéricas 1-10** (40 cartas), por decisión del usuario. Con `false` se usa el mazo completo de 56.
- Al sentarse (`PlayerSat`): espera 0.6 s, revela la primera carta numérica en el montón y reparte 4 cartas alternadas al jugador (a la mano) y al orco (boca abajo frente a él).
- La **mano** se coloca relativa a la cámara (`handDistance` 0.45, `handDrop` 0.17, `handSpacing` 0.095, abanico leve). La carta bajo el mouse se levanta.
- Sentado: **clic en una carta de la mano** = ponerla en el montón; **clic en el mazo** = agarrar una; **clic en la carta de arriba del montón** = recogerla. Arriba se muestran el contador (Mazo / Mesa / Mano) y la ayuda.
- Al levantarse (`PlayerStoodUp`): todas las cartas vuelan al mazo, se destruyen y se rearma y baraja el mazo.
- ⚠️ Es un prototipo para ver las animaciones: **no aplica el reglamento** (ni turnos, ni validación, ni IA del orco, ni puntos). Agarrar del mazo durante la ronda no existe en el reglamento.

### `CameraFollow.cs` (Player/)
- Cámara orbital en tercera persona, vista elevada tipo diorama (pitch 50°, distancia 6, FOV 40).
- Clic derecho + mouse para girar, rueda para zoom (2.5–9). Sigue al jugador con suavizado.
- **Vista de mesa al sentarse:** cuando el jugador se sienta, pasa en 0.5 s (`transitionSpeed` 2) a una vista **en primera persona** desde los ojos del personaje, mirando la mesa y al rival (`seatedBack` −0.15, `seatedHeight` 1.05, `seatedLookAhead` 0.85, `seatedLookHeight` 0.4, FOV 60). Así se ven a la vez la cara del orco, sus cartas, el centro de la mesa y la mano. Al levantarse vuelve a la órbita. Mientras está sentado se desactivan el giro y el zoom.
- Se eligió la primera persona porque, con los personajes cabezones, cualquier cámara detrás de la cabeza tapa la mesa. Da un estilo de juego de cartas de mesa (tipo *Inscryption*).

### `Openable.cs` (Gameplay/)
- Abre y cierra con el bool `Open` del Animator. Tiene un `blockingCollider` opcional que se desactiva al abrir (lo usa la reja).

### `Collectible.cs` (Gameplay/)
- Gira y flota. Al entrar en su trigger el jugador, suma a `GameHUD` según su tipo (`Coin`, `Key`, `Potion`) y se destruye.

### `GameHUD.cs` (UI/)
- HUD **provisional** con `OnGUI`: contador de monedas, llaves y pociones, más una línea de controles. Los contadores son estáticos y se reinician en `Awake`. Reemplazar por UI real (uGUI o UI Toolkit) cuando haya reglas.

### `InteractionPromptUI.cs` (UI/)
- Ayuda de la tecla E (provisional, con `OnGUI`): un panel oscuro con un dibujo de la tecla **E** y el texto de la acción.
- De pie: flota sobre `FocusedInteractable.PromptPosition` con un leve vaivén ("[E] Sentarse", "[E] Abrir" o "[E] Cerrar").
- Sentado: fijo **abajo a la izquierda** con "[E] Levantarse".

### `MiniDungeonBuilder.cs` (Editor/)
- Menú **Tools → Mini Dungeon → Construir escena demo** (y **Configurar modelos**).
- Importa los **TMP Essential Resources** si falta `Assets/TextMesh Pro` (las cartas usan TextMeshPro). La importación es asíncrona; el aviso sobre `LiberationSans.ttf.meta` es inofensivo.
- Configura los importadores (avatar Generic y loops de `idle`, `walk`, `sprint`, `sit` y `show-hide`), crea los AnimatorControllers en `_Game/Animations/` y construye `MiniDungeon.unity`.
- Se puede **volver a ejecutar**: borra y regenera los objetos raíz `Level`, `Player` y `GameSystems`. No toca la cámara, la luz ni el Global Volume. ⚠️ **Los cambios hechos a mano dentro de esos tres objetos se pierden al reconstruir.** Cuando el nivel deba editarse a mano, dejar de usar el builder para esa escena (o convertir las piezas en prefabs).
- Agrega `MiniDungeon` como escena 0 en Build Settings.

### Escena `MiniDungeon.unity`
- Sala de 9×9 baldosas (x, z de −4 a 4), muros en el anillo ±5. El muro sur es `wall-half` para que la cámara vea el interior.
- Al norte, `wall-opening` con una reja animada que se abre con E, y detrás un pasillo con escaleras.
- **Mesa de juego en el centro** (`table` en (0, 0.5)) con un `CardTable` (mazo y montón en el centro). Dos sillas con `Seat` en x = ±0.8 (constantes `TableZ` y `ChairDistance` del builder), mirando a la mesa. El **orco está sentado** en la silla este desde el inicio; el jugador se sienta en la oeste con E.
- Resto: 4 columnas, armería en el muro este, barriles, vasijas, rocas, estandartes, 2 cofres que se abren, trampa animada en bucle en (−3, −2), 6 monedas, 1 llave y 1 poción.
- El jugador aparece en (0, 0, −3) mirando al norte.

### Controles
WASD mover · Shift correr · Espacio saltar · Clic izquierdo atacar · E interactuar (abrir, sentarse y levantarse) · Clic derecho + mouse girar la cámara · Rueda zoom.

---

## 6. Diseño del juego (reglas)

> **Reglamento oficial v1.0:** `Docs/Juego_Cartas_Reglamento_Definitivo_v1.0.docx` (fuente de verdad, escrito por el equipo) y su transcripción completa en **[`Docs/Reglamento.md`](Docs/Reglamento.md)**. Las decisiones nuevas se anotan abajo con su fecha.

### Resumen del reglamento (v1.0)
- **Mazo de 56 cartas:** números 1-10 (4 copias de cada uno = 40) + 4 tipos de comodín × 4 = 16: *Cambio de sentido*, *Invertir comparación*, *Salto* y *Fijar valor*.
- **Ronda:** se reparten **4 cartas** a cada jugador y se revela una **carta numérica inicial** en la mesa (los comodines que salgan se apartan y vuelven a la reserva). La comparación empieza **ascendente (>=)** y el sentido **horario**. El jugador inicial es al azar en la primera ronda y luego rota un asiento en sentido horario.
- **Estado público de la mesa:** valor de referencia (1-10), comparación (>= o <=) y sentido (horario o antihorario).
- **Turno:** hay que jugar una carta numérica válida según la comparación (pasa a ser la nueva referencia), **varias copias del mismo número** a la vez, o un **comodín**, que se puede jugar siempre. **No se puede pasar.** Si no hay jugada legal, la ronda termina por **bloqueo**.
- **Comodines:** Cambio de sentido invierte el sentido. Invertir comparación cambia >= ↔ <=. Salto hace perder el turno al siguiente (con 2 jugadores, quien lo juega vuelve a jugar). Fijar valor elige una referencia del 1 al 10.
- **Fin de ronda:** por **vaciado** (quedarse con 0 cartas tras una jugada legal) = **3 puntos**. Por **bloqueo** = **1 punto** para quien tenga menos cartas; si empatan, gana la menor suma de valores, con cada comodín contando 10. Si persiste el empate, 1 punto para cada empatado.
- **Persistencia:** todas las cartas de una ronda salen de la partida, así que cada ronda consume 4m + 1 cartas. La partida termina cuando no alcanzan para otra ronda, y gana quien tenga más puntos (puede haber empate).
- **En Unity se juega 1 contra 1** (jugador contra orco): con 56 cartas y 9 cartas por ronda hay **6 rondas**.

### Decisiones tomadas
- 2026-10-16 — El juego es **3D**, en **tercera persona con cámara elevada** (estilo diorama).
- 2026-10-16 — Ambientación inicial: **calabozo / mazmorra** (Kenney Mini Dungeon). Protagonista: `character-human`. El orco es candidato a enemigo.
- 2026-10-16 — Mecánicas prototipadas, todavía sin reglas: moverse, correr, saltar, atacar (solo animación, sin daño), abrir cofres y rejas, y recoger monedas, llaves y pociones.

- 2026-10-16 — **El juego es un juego de cartas en 3D.** Las reglas concretas y cómo va a funcionar se definen más adelante.
- 2026-10-16 — Hay una **mesa de juego en el centro del calabozo**. El orco espera sentado en un extremo y el jugador se sienta en el otro con E. Ahí se jugarán las partidas.
- 2026-10-16 — Al acercarse a la silla aparece la ayuda "[E] Sentarse". Sentado, aparece "[E] Levantarse" abajo a la izquierda.
- 2026-10-16 — Al sentarse, la cámara pasa a **primera persona mirando la mesa y al rival**, para que se sienta como un juego de cartas.

- 2026-10-23 — Se adopta el **Reglamento v1.0** (ver arriba).
- 2026-10-23 — En el **centro de la mesa** están el **mazo** (cartas para agarrar) y, al lado, el **montón de cartas jugadas** en la ronda. Al sentarse, el jugador recibe su mano y el orco la suya (boca abajo, frente a él).
- 2026-10-23 — **Por ahora solo cartas numéricas del 1 al 10** (sin comodines). `CardTable.numbersOnly`.
- 2026-10-23 — Prototipo visual de cartas: diseño **genérico** de momento. Se puede agarrar del mazo, poner una carta en el montón y recoger la de arriba del montón, para ver las animaciones. **Todavía no se aplican las reglas** (validación de jugadas, turnos, IA del orco, puntos).

### Preguntas abiertas
- Diseño visual definitivo de las cartas (ilustraciones, colores, símbolos de los comodines).
- Interfaz para elegir el valor con *Fijar valor* y para jugar varias copias iguales a la vez.
- ¿Hay varios rivales o mesas? ¿Se mantienen el movimiento, el ataque y los recogibles, o solo sirven de ambientación? ¿Monedas para apostar o comprar cartas?
- ¿Un solo nivel o varios? ¿Hay tiempo límite o puntaje?
- Plataforma de entrega: build de Windows (.exe) o WebGL.

---

## 7. Problemas conocidos y notas técnicas

- **Probar teclas desde el MCP:** las teclas simuladas (`InputSystem.QueueStateEvent`) solo llegan a las acciones cuando la ventana *Game* tiene el foco, y las capturas de pantalla del MCP se lo quitan. Para comprobar la lógica, llamar los métodos directamente (por reflexión) y pedirle al usuario que pruebe los controles reales.
- A la acción `Player/Interact` se le **quitó la interacción "Hold"** que traía la plantilla, para que E funcione con un toque.
- Las capturas del MCP se guardan en `Temp/Screens` (pasar `output_folder`). `Assets/Screenshots/` está en `.gitignore`.
- El atacar no hace daño todavía. El orco no tiene IA ni vida.
- Las paredes no ocultan al jugador si se interponen; con la cámara alta casi no pasa. Si llega a ser un problema, hacer transparentes las paredes cercanas a la cámara.
- `ProjectVersion.txt` no existía al crear el proyecto (por el fallo de batchmode); ya lo generó Unity 6000.6.6f1.

---

## 8. Créditos de assets

| Asset | Fuente | Licencia |
|---|---|---|
| Mini Dungeon | Kenney (www.kenney.nl) | CC0 |
| Animated Characters Protagonists | Kenney (www.kenney.nl) | CC0 |

---

## 9. Próximos pasos sugeridos

1. El usuario prueba la escena MiniDungeon (incluido sentarse a la mesa) y da retroalimentación.
2. Definir las reglas del juego de cartas (§6) y escribir el GDD en `Docs/`.
3. Motor de reglas sobre `CardTable`: estado de la mesa (valor, comparación, sentido), turnos jugador ↔ orco, validación de jugadas (incluidas varias copias iguales), bloqueo/vaciado, puntos y las 6 rondas con persistencia del mazo. Quitar "agarrar del mazo" cuando entren las reglas.
4. IA del orco, comodines con su interfaz (Fijar valor), diseño final de cartas, menú principal, pausa, UI real, sonido y una build de Windows.
5. Evaluar la actualización de MCP for Unity a v10.3.0.

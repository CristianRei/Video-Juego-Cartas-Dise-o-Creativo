# CLAUDE.md — Videojuego 3D "Diseño Creativo"

> Documento vivo. **Léelo antes de trabajar en el proyecto y actualízalo cuando cambie algo importante**: reglas del juego acordadas, scripts nuevos, decisiones técnicas o problemas conocidos.
> Idioma del proyecto: **español** (conversación, comentarios en el código, commits y documentación).

---

## 1. Contexto

- Proyecto para la materia universitaria **Diseño Creativo**. El objetivo es entregar un **videojuego en 3D** (requisito del profesor).
- El usuario no es programador experto: explicarle los pasos en Unity de forma concreta (qué menú, qué botón) y sin jerga innecesaria.
- **Estado actual:** prototipo jugable de movimiento en un mini calabozo. **Las reglas del juego todavía no están definidas** (ver §6). Mientras tanto se adelanta la base técnica, que sirve para casi cualquier juego.

---

## 2. Herramientas y versiones

| Herramienta | Versión / nota |
|---|---|
| Unity | **6000.6.6f1** (Unity 6.6). El proyecto se migró desde 6000.5.9f1; ya no usar esa versión |
| Render pipeline | **URP** (plantilla Universal 3D). Assets de render en `Assets/Settings/` (`PC_RPAsset`, `Mobile_RPAsset`) |
| Graphics API | **DirectX 11** a propósito, por compatibilidad con PCs de la escuela. No activar Auto Graphics API (DX12) |
| Input | **Input System nuevo** únicamente (`activeInputHandler: 1`). Acciones del proyecto en `Assets/InputSystem_Actions.inputactions` |
| MCP | **MCP for Unity** (CoplayDev) `v10.0.0`, transporte HTTP local `http://127.0.0.1:8080`. Hay actualización a v10.3.0 pendiente; no actualizar sin comprobar antes |
| Control de versiones | Git + Git LFS. Repo en la raíz `Diseño Creativo/`, rama `main`, sin remoto por ahora |
| Blender | **No se usa por ahora.** Decisión del usuario: los modelos 3D se descargan de internet |

### Usar el MCP de Unity
1. Unity debe estar **abierto** con el proyecto `JuegoDisenoCreativo`.
2. En Unity: **Window → MCP for Unity → Start Server**, hasta que el indicador quede en **verde**.
3. Hacer esto **antes** de abrir la sesión de Claude. Si el servidor se apaga (por ejemplo, porque el PC entró en reposo), las herramientas `mcp__UnityMCP__*` responden `no_unity_session` y hay que pedirle al usuario que lo vuelva a encender.
- `execute_code` compila con CodeDom (**C# 6**): no usar LINQ sin `using`, ni tuplas, ni pattern matching moderno dentro de ese código. Los scripts `.cs` del proyecto sí pueden usar C# moderno.
- Después de crear o editar scripts: `refresh_unity` → `read_console` para ver errores de compilación.
- Unity **no puede ejecutarse en batchmode** (`-batchmode`) en este PC: falla la licencia. Todo se hace con el editor abierto y el MCP.

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
├── Docs/                           ← documentos de diseño (GDD, reglas, notas para el profe)
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
            │   ├── Gameplay/       Openable.cs, Collectible.cs
            │   ├── UI/             GameHUD.cs
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
- Interactuar: `OverlapSphere` frente al jugador; llama a `Openable.Toggle()` en el primero que encuentre.
- Parámetros del Animator: `Speed` (float), `Grounded` (bool), `Jump`, `Attack`, `Interact` (triggers).

### `CameraFollow.cs` (Player/)
- Cámara orbital en tercera persona, vista elevada tipo diorama (pitch 50°, distancia 6, FOV 40).
- Clic derecho + mouse para girar, rueda para zoom (2.5–9). Sigue al jugador con suavizado.

### `Openable.cs` (Gameplay/)
- Abre y cierra con el bool `Open` del Animator. Tiene un `blockingCollider` opcional que se desactiva al abrir (lo usa la reja).

### `Collectible.cs` (Gameplay/)
- Gira y flota. Al entrar en su trigger el jugador, suma a `GameHUD` según su tipo (`Coin`, `Key`, `Potion`) y se destruye.

### `GameHUD.cs` (UI/)
- HUD **provisional** con `OnGUI`: contador de monedas, llaves y pociones, más una línea de controles. Los contadores son estáticos y se reinician en `Awake`. Reemplazar por UI real (uGUI o UI Toolkit) cuando haya reglas.

### `MiniDungeonBuilder.cs` (Editor/)
- Menú **Tools → Mini Dungeon → Construir escena demo** (y **Configurar modelos**).
- Configura los importadores (avatar Generic y loops de `idle`, `walk`, `sprint` y `show-hide`), crea los AnimatorControllers en `_Game/Animations/` y construye `MiniDungeon.unity`.
- Se puede **volver a ejecutar**: borra y regenera los objetos raíz `Level`, `Player` y `GameSystems`. No toca la cámara, la luz ni el Global Volume. ⚠️ **Los cambios hechos a mano dentro de esos tres objetos se pierden al reconstruir.** Cuando el nivel deba editarse a mano, dejar de usar el builder para esa escena (o convertir las piezas en prefabs).
- Agrega `MiniDungeon` como escena 0 en Build Settings.

### Escena `MiniDungeon.unity`
- Sala de 9×9 baldosas (x, z de −4 a 4), muros en el anillo ±5. El muro sur es `wall-half` para que la cámara vea el interior.
- Al norte, `wall-opening` con una reja animada que se abre con E, y detrás un pasillo con escaleras.
- Contenido: 4 columnas, comedor (mesa, sillas, espada), armería en el muro este, barriles, vasijas, rocas, estandartes, 2 cofres que se abren, trampa animada en bucle, un orco quieto en idle (sin IA), 6 monedas, 1 llave y 1 poción.
- El jugador aparece en (0, 0, −3) mirando al norte.

### Controles
WASD mover · Shift correr · Espacio saltar · Clic izquierdo atacar · E interactuar · Clic derecho + mouse girar la cámara · Rueda zoom.

---

## 6. Diseño del juego (reglas)

> **Pendiente de definir por el equipo.** Cada regla que se acuerde en una conversación se anota aquí con la fecha.

### Decisiones tomadas
- 2026-10-16 — El juego es **3D**, en **tercera persona con cámara elevada** (estilo diorama).
- 2026-10-16 — Ambientación inicial: **calabozo / mazmorra** (Kenney Mini Dungeon). Protagonista: `character-human`. El orco es candidato a enemigo.
- 2026-10-16 — Mecánicas prototipadas, todavía sin reglas: moverse, correr, saltar, atacar (solo animación, sin daño), abrir cofres y rejas, y recoger monedas, llaves y pociones.

### Preguntas abiertas
- Género y objetivo del jugador: ¿qué hay que hacer para ganar? ¿Cómo se pierde?
- ¿Hay combate real (vida, daño, enemigos con IA)? ¿Para qué sirven monedas, llaves y pociones?
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

1. El usuario prueba la escena MiniDungeon y da retroalimentación sobre velocidad, cámara y colisiones.
2. Definir las reglas del juego (§6) y escribir el GDD en `Docs/`.
3. Según las reglas: vida y daño, IA del orco (NavMesh), menú principal, pausa, UI real, sonido y una build de Windows.
4. Evaluar la actualización de MCP for Unity a v10.3.0.

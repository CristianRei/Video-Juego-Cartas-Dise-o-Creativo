# Diseño Creativo — Videojuego 3D

Proyecto de la materia Diseño Creativo.

## Estructura

```
Diseño Creativo/
├── JuegoDisenoCreativo/     Proyecto de Unity 6 (URP)
│   └── Assets/_Game/        Todo lo nuestro va aquí
│       ├── Scenes/          Escenas (.unity)
│       ├── Scripts/         Código C#
│       │   ├── Player/      Movimiento, cámara, input del jugador
│       │   ├── Gameplay/    Reglas, enemigos, objetos, puntuación
│       │   ├── UI/          Menús, HUD
│       │   ├── Managers/    GameManager, audio, carga de escenas
│       │   └── Editor/      Herramientas del editor
│       ├── Models/          Modelos 3D importados (.fbx / .glb)
│       ├── Materials/
│       ├── Textures/
│       ├── Prefabs/
│       ├── Animations/
│       ├── Audio/           Music/ y SFX/
│       └── UI/              Sprites e íconos de interfaz
├── Blender/                 Archivos fuente .blend y scripts que generan modelos
└── Docs/                    Reglas del juego, GDD, notas para el profe
```

## Requisitos

- Unity 6 (6000.6.6f1) desde Unity Hub
- Blender 4.x (para los modelos)
- Git + Git LFS

using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Construye la escena de prueba "MiniDungeon" con los modelos de Kenney Mini Dungeon.
// Menú: Tools > Mini Dungeon > Construir escena demo. Se puede volver a ejecutar:
// borra y regenera "Level", "Player" y "GameSystems" sin tocar cámara ni luz.
public static class MiniDungeonBuilder
{
    const string ModelDir = "Assets/_Game/Models/MiniDungeon/";
    const string AnimDir = "Assets/_Game/Animations/";
    const string ScenePath = "Assets/_Game/Scenes/MiniDungeon.unity";
    const string BaseScenePath = "Assets/_Game/Scenes/Main.unity";

    const float WalkSpeed = 1.6f;
    const float SprintSpeed = 3.2f;

    // Mesa de juego y sillas (las sillas van un poco separadas para que quepa el personaje sentado).
    const float TableZ = 0.5f;
    const float ChairDistance = 0.8f;
    // La animación "sit" de Kenney es para sentarse en el suelo (cadera ~0.11 bajo el origen);
    // el asiento de la silla está a 0.19, así que se sube 0.30.
    static readonly Vector3 SitOffset = new Vector3(0f, 0.30f, 0.03f);

    static Transform level;
    static System.Random rng;

    [MenuItem("Tools/Mini Dungeon/Configurar modelos")]
    public static void ConfigureModels()
    {
        ConfigureAnimated("character-human", "idle", "walk", "sprint", "sit");
        ConfigureAnimated("character-orc", "idle", "walk", "sprint", "sit");
        ConfigureAnimated("chest");
        ConfigureAnimated("gate");
        ConfigureAnimated("trap", "show-hide");
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Mini Dungeon/Construir escena demo")]
    public static void Build()
    {
        ConfigureModels();
        if (!AssetDatabase.IsValidFolder(AnimDir.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/_Game", "Animations");

        var characterController = CreateCharacterController("character-human", "Player");
        var orcController = CreateCharacterController("character-orc", "Orc");
        var chestController = CreateOpenableController("chest", "Chest");
        var gateController = CreateOpenableController("gate", "Gate");
        var trapController = CreateLoopController("trap", "show-hide", "Trap");

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            AssetDatabase.CopyAsset(BaseScenePath, ScenePath);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Level" || root.name == "Player" || root.name == "GameSystems")
                Object.DestroyImmediate(root);

        rng = new System.Random(7);
        level = new GameObject("Level").transform;

        BuildRoom();
        BuildProps(chestController, gateController, trapController, orcController);
        var player = BuildPlayer(characterController);
        SetupCamera(player);

        var systems = new GameObject("GameSystems");
        systems.AddComponent<GameHUD>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log("[MiniDungeon] Escena construida: " + ScenePath);
    }

    // ---------- Importación y animación ----------

    static void ConfigureAnimated(string model, params string[] loopingClips)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelDir + model + ".fbx");
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
            clip.loopTime = loopingClips.Contains(clip.name);
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    static AnimationClip Clip(string model, string name)
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(ModelDir + model + ".fbx")
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == name);
        if (clip == null) Debug.LogError($"[MiniDungeon] No se encontró la animación '{name}' en {model}");
        return clip;
    }

    static AnimatorController NewController(string name)
    {
        string path = AnimDir + name + ".controller";
        AssetDatabase.DeleteAsset(path);
        return AnimatorController.CreateAnimatorControllerAtPath(path);
    }

    static AnimatorController CreateCharacterController(string model, string name)
    {
        var controller = NewController(name);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Interact", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Sit", AnimatorControllerParameterType.Bool);

        var locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(Clip(model, "idle"), 0f);
        tree.AddChild(Clip(model, "walk"), WalkSpeed);
        tree.AddChild(Clip(model, "sprint"), SprintSpeed);

        var sm = controller.layers[0].stateMachine;
        AddOneShot(sm, locomotion, "Jump", Clip(model, "jump"), "Jump");
        AddOneShot(sm, locomotion, "Attack", Clip(model, "attack-melee-right"), "Attack");
        AddOneShot(sm, locomotion, "Interact", Clip(model, "interact-right"), "Interact");

        var sit = sm.AddState("Sit");
        sit.motion = Clip(model, "sit");
        var toSit = locomotion.AddTransition(sit);
        toSit.AddCondition(AnimatorConditionMode.If, 0f, "Sit");
        toSit.hasExitTime = false;
        toSit.duration = 0.1f;
        var fromSit = sit.AddTransition(locomotion);
        fromSit.AddCondition(AnimatorConditionMode.IfNot, 0f, "Sit");
        fromSit.hasExitTime = false;
        fromSit.duration = 0.1f;
        return controller;
    }

    static void AddOneShot(AnimatorStateMachine sm, AnimatorState from, string stateName, AnimationClip clip, string trigger)
    {
        var state = sm.AddState(stateName);
        state.motion = clip;
        var enter = from.AddTransition(state);
        enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        enter.hasExitTime = false;
        enter.duration = 0.05f;
        var exit = state.AddTransition(from);
        exit.hasExitTime = true;
        exit.exitTime = 0.85f;
        exit.duration = 0.15f;
    }

    static AnimatorController CreateOpenableController(string model, string name)
    {
        var controller = NewController(name);
        controller.AddParameter("Open", AnimatorControllerParameterType.Bool);
        var sm = controller.layers[0].stateMachine;
        var closed = sm.AddState("Closed");
        var open = sm.AddState("Open");
        open.motion = Clip(model, "open");
        var close = sm.AddState("Close");
        close.motion = Clip(model, "close");
        sm.defaultState = closed;
        Link(closed, open, AnimatorConditionMode.If);
        Link(close, open, AnimatorConditionMode.If);
        Link(open, close, AnimatorConditionMode.IfNot);
        return controller;
    }

    static void Link(AnimatorState from, AnimatorState to, AnimatorConditionMode mode)
    {
        var t = from.AddTransition(to);
        t.AddCondition(mode, 0f, "Open");
        t.hasExitTime = false;
        t.duration = 0f;
    }

    static AnimatorController CreateLoopController(string model, string clip, string name)
    {
        var controller = NewController(name);
        var state = controller.layers[0].stateMachine.AddState(clip);
        state.motion = Clip(model, clip);
        return controller;
    }

    // ---------- Colocación ----------

    static GameObject Place(string model, float x, float z, float rotY = 0f, float y = 0f, Transform parent = null, string collider = "mesh")
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + model + ".fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent != null ? parent : level);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (collider == "mesh")
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }
        else if (collider == "box")
        {
            var b = WorldBounds(go);
            var box = go.AddComponent<BoxCollider>();
            box.center = b.center;
            box.size = b.size;
        }

        go.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, rotY, 0f));
        return go;
    }

    static Bounds WorldBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    static Animator SetupAnimator(GameObject go, string model, RuntimeAnimatorController controller)
    {
        var animator = go.GetComponent<Animator>();
        if (animator == null) animator = go.AddComponent<Animator>();
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelDir + model + ".fbx").OfType<Avatar>().FirstOrDefault();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        return animator;
    }

    // ---------- Escenario ----------

    // Sala de 9x9 baldosas (x,z de -4 a 4) rodeada de muros en el anillo ±5.
    // El muro sur es bajo (wall-half) para que la cámara vea el interior.
    // Al norte hay una reja que da a un pasillo con escaleras.
    static void BuildRoom()
    {
        var floor = new GameObject("Floor").transform;
        floor.SetParent(level);
        var walls = new GameObject("Walls").transform;
        walls.SetParent(level);

        for (int x = -4; x <= 4; x++)
            for (int z = -4; z <= 4; z++)
                Place(rng.NextDouble() < 0.2 ? "floor-detail" : "floor", x, z, 90f * rng.Next(4), 0f, floor);

        for (int i = -5; i <= 5; i++)
        {
            Place("wall-half", i, -5, 0f, 0f, walls);
            Place(i == 0 ? "wall-opening" : "wall", i, 5, 0f, 0f, walls);
        }
        for (int i = -4; i <= 4; i++)
        {
            Place("wall", -5, i, 0f, 0f, walls);
            Place("wall", 5, i, 0f, 0f, walls);
        }

        // Pasillo al norte con escaleras.
        Place("floor", 0, 6, 0f, 0f, floor);
        Place("stairs", 0, 7, 0f, 0f, walls);
        foreach (int z in new[] { 6, 7 })
        {
            Place("wall", -1, z, 0f, 0f, walls);
            Place("wall", 1, z, 0f, 0f, walls);
        }
        Place("dirt", 0, 8, 0f, 0f, walls);
    }

    static void BuildProps(AnimatorController chest, AnimatorController gate, AnimatorController trap, AnimatorController orc)
    {
        var props = new GameObject("Props").transform;
        props.SetParent(level);
        var pickups = new GameObject("Pickups").transform;
        pickups.SetParent(level);

        // Reja en la abertura del muro norte (se abre con E).
        var gateGo = Place("gate", 0, 5, 0f, 0f, props, "none");
        SetupAnimator(gateGo, "gate", gate);
        var gateBlock = gateGo.AddComponent<BoxCollider>();
        gateBlock.center = new Vector3(0f, 0.5f, 0f);
        gateBlock.size = new Vector3(1f, 1f, 0.3f);
        gateGo.AddComponent<Openable>().blockingCollider = gateBlock;

        // Columnas.
        foreach (var p in new[] { (-2, 2), (2, 2), (-2, -2), (2, -2) })
            Place("column", p.Item1, p.Item2, 0f, 0f, props);

        // Mesa de juego en el centro: el jugador se sienta al oeste (E), el orco al este.
        Place("table", 0, TableZ, 0f, 0f, props);
        AddSeat(-ChairDistance, 90f, props);
        var orcSeat = AddSeat(ChairDistance, -90f, props);

        // Armería en el muro este.
        Place("shield-round", 4.4f, 0f, -90f, 0.19f, props, "none");
        Place("shield-rectangle", 4.4f, 0.6f, -90f, 0f, props, "none");
        Place("weapon-spear", 4.4f, -0.6f, 0f, 0f, props, "none");

        // Almacén (suroeste y noroeste).
        Place("barrel", -4, -4, 0f, 0f, props);
        Place("barrel", -3.4f, -4, 30f, 0f, props);
        Place("barrel", -4, -3.4f, 60f, 0f, props);
        Place("pot", -4, 4, 0f, 0f, props);
        Place("pot", -3.3f, 4.1f, 45f, 0f, props);
        Place("rocks", -4, 2, 0f, 0f, props);
        Place("stones", 1, 3.5f, 0f, 0f, props);
        Place("wood-structure", -2, 0, 0f, 0f, props, "none");
        Place("wood-support", 2, 0, 90f, 0f, props, "none");

        // Estandartes colgados en los muros.
        Place("banner", -2, 5, 0f, 0f, props, "none");
        Place("banner", 2, 5, 0f, 0f, props, "none");
        Place("banner", -5, 0, -90f, 0f, props, "none");

        // Cofres que se abren con E.
        foreach (var (x, z, rot) in new[] { (-4f, 0f, 90f), (4f, 3.9f, 180f) })
        {
            var c = Place("chest", x, z, rot, 0f, props, "box");
            SetupAnimator(c, "chest", chest);
            c.AddComponent<Openable>();
        }

        // Trampa animada.
        var trapGo = Place("trap", -3, -2, 0f, 0f, props, "none");
        SetupAnimator(trapGo, "trap", trap);

        // Orco sentado a la mesa (rival de cartas).
        var orcGo = Place("character-orc", 0f, 0f, 0f, 0f, props, "none");
        orcGo.name = "Orc";
        orcSeat.seatedAtStart = SetupAnimator(orcGo, "character-orc", orc);
        orcGo.transform.SetPositionAndRotation(orcSeat.SitPosition, orcSeat.SitRotation);

        // Recogibles.
        foreach (var p in new[] { (-1f, -3f), (1f, -3f), (0f, 2f), (-3f, 1f), (3f, 1.5f), (-1f, -1.5f) })
            AddPickup("coin", p.Item1, p.Item2, Collectible.Kind.Coin, pickups);
        AddPickup("key", -3.2f, 0f, Collectible.Kind.Key, pickups);
        AddPickup("potion", 2f, -4f, Collectible.Kind.Potion, pickups);
    }

    static Seat AddSeat(float x, float rotY, Transform parent)
    {
        var chair = Place("chair", x, TableZ, rotY, 0f, parent);
        var seat = chair.AddComponent<Seat>();
        seat.sitOffset = SitOffset;
        return seat;
    }

    static void AddPickup(string model, float x, float z, Collectible.Kind kind, Transform parent)
    {
        var go = Place(model, x, z, 0f, 0.12f, parent, "none");
        var trigger = go.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.3f;
        trigger.center = new Vector3(0f, 0.2f, 0f);
        go.AddComponent<Collectible>().kind = kind;
    }

    static GameObject BuildPlayer(AnimatorController controller)
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0.05f, -3f);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 0.7f;
        cc.radius = 0.2f;
        cc.center = new Vector3(0f, 0.37f, 0f);
        cc.stepOffset = 0.2f;
        cc.skinWidth = 0.02f;

        var model = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "character-human.fbx"), player.transform);
        model.name = "Model";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        SetupAnimator(model, "character-human", controller);

        var pc = player.AddComponent<PlayerController>();
        pc.walkSpeed = WalkSpeed;
        pc.sprintSpeed = SprintSpeed;
        return player;
    }

    static void SetupCamera(GameObject player)
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.17f, 0.17f, 0.21f);
        cam.fieldOfView = 40f;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player.transform;
        follow.distance = 6f;
        follow.pitch = 50f;
        follow.yaw = 0f;
        player.GetComponent<PlayerController>().cameraTransform = cam.transform;

        Quaternion rot = Quaternion.Euler(follow.pitch, follow.yaw, 0f);
        cam.transform.SetPositionAndRotation(player.transform.position + follow.targetOffset - rot * Vector3.forward * follow.distance, rot);
    }
}

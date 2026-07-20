using UnityEngine;

// Central conductor: menu state, chapter start, win/lose, save (GDD sections 9/13/18).
public class GameManager : MonoBehaviour
{
    public enum GameState { Menu, Playing, Won, Dead, Paused }

    public static GameManager Instance { get; private set; }

    // Full campaign order (GDD section 22) — all five eras playable.
    public static readonly WarEra[] Chapters =
        { WarEra.Medieval, WarEra.WorldWarOne, WarEra.WorldWarTwo, WarEra.Modern, WarEra.Future };

    public GameState State { get; private set; } = GameState.Menu;
    public bool IsPlaying { get { return State == GameState.Playing; } }
    public EraManager eraManager;
    public SaveData save;
    public PlayerController Player { get; private set; }
    public Health PlayerHealth { get; private set; }
    public MissionObjective Objective { get; private set; }
    public int CurrentChapter { get; private set; }

    Camera cam;
    Light sun;
    GameObject levelRoot;
    GameObject playerRoot;

    // Safety net: boots the game even if the scene ever loads empty/damaged.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance == null && FindFirstObjectByType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        eraManager = gameObject.AddComponent<EraManager>();
        gameObject.AddComponent<GameHUD>();
        save = SaveSystem.Load();

        cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        sun = FindFirstObjectByType<Light>();
        if (sun == null || sun.type != LightType.Directional)
        {
            var sunGo = new GameObject("Directional Light");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        EnterMenu();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (State == GameState.Playing) Pause(true);
            else if (State == GameState.Paused) Pause(false);
        }
    }

    public bool IsChapterUnlocked(int index)
    {
        return index <= save.unlockedChapter;
    }

    public void StartChapter(int index)
    {
        CurrentChapter = index;
        WarEra era = Chapters[index];
        eraManager.currentEra = era;
        Time.timeScale = 1f;

        ClearLevel();
        levelRoot = LevelBuilder.Build(era, sun, out Vector3 playerSpawn, out Vector3[] enemySpawns);

        // Player (GDD section 9: one shared framework, era abilities toggled)
        playerRoot = new GameObject("Player");
        playerRoot.transform.position = playerSpawn;
        var cc = playerRoot.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.45f;
        PlayerHealth = playerRoot.AddComponent<Health>();
        PlayerHealth.maxHealth = 100f;
        PlayerHealth.regenRate = 9f;   // forgiving shooter regen after 4s out of fire
        Player = playerRoot.AddComponent<PlayerController>();
        Player.advancedMovement = eraManager.CanUseAdvancedMovement();
        Player.AttachCamera(cam.transform);
        cam.fieldOfView = 70f;
        PlayerHealth.onDeath += OnPlayerDeath;
        Player.SetWeapons(CreateLoadout(era, cc));

        foreach (Vector3 spawn in enemySpawns)
            LevelBuilder.SpawnEnemy(era, spawn, playerRoot.transform, levelRoot.transform);

        // Objective (GDD section 13)
        var objGo = new GameObject("Objective");
        objGo.transform.SetParent(levelRoot.transform);
        var obj = objGo.AddComponent<EliminateTargetsObjective>();
        obj.objectiveDescription = "Eliminate all hostiles";
        obj.totalTargets = enemySpawns.Length;
        obj.onCompleted += OnObjectiveComplete;
        Objective = obj;

        State = GameState.Playing;
        SetCursorLocked(true);
    }

    // Two weapons per era (GDD section 20): a ranged + melee pair, each with a
    // first-person viewmodel. Switch with 1/2 or the mouse wheel.
    System.Collections.Generic.List<WeaponBase> CreateLoadout(WarEra era, Collider playerCollider)
    {
        var list = new System.Collections.Generic.List<WeaponBase>();

        // Ray origin sits ahead of the camera so shots clear the player's own collider.
        var firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(cam.transform);
        firePoint.localPosition = new Vector3(0f, 0f, 0.6f);
        firePoint.localRotation = Quaternion.identity;

        Color steel = new Color(0.65f, 0.66f, 0.7f);
        Color wood = new Color(0.36f, 0.24f, 0.13f);
        Color gunmetal = new Color(0.2f, 0.2f, 0.22f);
        Color cyan = new Color(0.3f, 0.95f, 1f);

        switch (era)
        {
            case WarEra.Medieval:
                list.Add(Melee("Longsword", 34f, 1.7f, 2.4f, ViewModelKind.Sword, steel, steel));
                var bow = NewWeapon<ProjectileWeapon>("War Bow");
                bow.damage = 55f;
                bow.attackRate = 1.1f;
                bow.launchSpeed = 42f;
                bow.firePoint = firePoint;
                bow.ownerCollider = playerCollider;
                bow.viewModel = WeaponViewModel.Create(cam, ViewModelKind.Bow, wood, wood, false);
                list.Add(bow);
                break;

            case WarEra.WorldWarOne:
                list.Add(Firearm("Bolt-Action Rifle", 45f, 0.9f, 5, 45, 2.3f, 0.008f, false,
                    new Color(1f, 0.8f, 0.4f), ViewModelKind.Rifle, wood, gunmetal, false, firePoint));
                list.Add(Melee("Trench Knife", 22f, 2.6f, 1.9f, ViewModelKind.Knife, steel, steel));
                break;

            case WarEra.WorldWarTwo:
                list.Add(Firearm("Service Rifle", 26f, 3.2f, 8, 72, 1.7f, 0.012f, false,
                    new Color(1f, 0.85f, 0.45f), ViewModelKind.Rifle, wood, gunmetal, false, firePoint));
                list.Add(Firearm("Sidearm", 15f, 4.5f, 7, 42, 1.2f, 0.02f, false,
                    new Color(1f, 0.85f, 0.45f), ViewModelKind.Pistol, gunmetal, gunmetal, false, firePoint));
                break;

            case WarEra.Modern:
                list.Add(Firearm("Assault Carbine", 11f, 9f, 30, 180, 1.6f, 0.025f, true,
                    new Color(1f, 0.9f, 0.6f), ViewModelKind.Rifle, gunmetal, new Color(0.28f, 0.3f, 0.28f), false, firePoint));
                list.Add(Melee("Combat Knife", 26f, 3f, 1.9f, ViewModelKind.Knife, steel, steel));
                break;

            default:  // Future
                list.Add(Firearm("Plasma Rifle", 16f, 7f, 30, 180, 1.3f, 0.02f, true,
                    cyan, ViewModelKind.Rifle, gunmetal, cyan, true, firePoint));
                list.Add(Melee("Energy Blade", 45f, 2f, 2.5f, ViewModelKind.EnergyBlade, gunmetal, cyan));
                break;
        }
        return list;
    }

    T NewWeapon<T>(string name) where T : WeaponBase
    {
        var go = new GameObject("Weapon_" + name);
        go.transform.SetParent(cam.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        var w = go.AddComponent<T>();
        w.weaponName = name;
        return w;
    }

    MeleeWeapon Melee(string name, float damage, float rate, float radius, ViewModelKind kind, Color main, Color accent)
    {
        var m = NewWeapon<MeleeWeapon>(name);
        m.damage = damage;
        m.attackRate = rate;
        m.attackRadius = radius;
        m.origin = cam.transform;
        m.viewModel = WeaponViewModel.Create(cam, kind, main, accent, kind == ViewModelKind.EnergyBlade);
        return m;
    }

    FirearmWeapon Firearm(string name, float damage, float rate, int mag, int reserve, float reload,
        float spread, bool auto, Color tracer, ViewModelKind kind, Color main, Color accent,
        bool emissive, Transform firePoint)
    {
        var f = NewWeapon<FirearmWeapon>(name);
        f.damage = damage;
        f.attackRate = rate;
        f.magazineSize = mag;
        f.ammunition = mag;
        f.reserveAmmo = reserve;
        f.reloadDuration = reload;
        f.spread = spread;
        f.autoFire = auto;
        f.tracerColor = tracer;
        f.firePoint = firePoint;
        f.viewModel = WeaponViewModel.Create(cam, kind, main, accent, emissive);
        return f;
    }

    void OnObjectiveComplete()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        if (CurrentChapter == save.unlockedChapter && save.unlockedChapter < Chapters.Length - 1)
            save.unlockedChapter++;
        save.chaptersCompleted = Mathf.Max(save.chaptersCompleted, CurrentChapter + 1);
        SaveSystem.Save(save);
        SetCursorLocked(false);
    }

    void OnPlayerDeath()
    {
        if (State != GameState.Playing) return;
        State = GameState.Dead;
        SetCursorLocked(false);
    }

    public void RetryChapter() { StartChapter(CurrentChapter); }

    public void EnterMenu()
    {
        ClearLevel();
        Time.timeScale = 1f;
        State = GameState.Menu;
        RenderSettings.fog = false;
        cam.transform.SetParent(null);
        cam.transform.position = new Vector3(0f, 14f, -26f);
        cam.transform.rotation = Quaternion.Euler(24f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
        SetCursorLocked(false);
    }

    void Pause(bool paused)
    {
        State = paused ? GameState.Paused : GameState.Playing;
        Time.timeScale = paused ? 0f : 1f;
        SetCursorLocked(!paused);
    }

    public void ResumeFromPause() { Pause(false); }

    void ClearLevel()
    {
        EnemyAI.Alive.Clear();
        if (cam != null)
        {
            cam.transform.SetParent(null);
            // weapons, viewmodels and fire points live under the camera — clear them
            for (int i = cam.transform.childCount - 1; i >= 0; i--)
                Destroy(cam.transform.GetChild(i).gameObject);
        }
        if (levelRoot != null) Destroy(levelRoot);
        if (playerRoot != null) Destroy(playerRoot);
        cam.clearFlags = CameraClearFlags.Skybox;
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

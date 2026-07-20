using UnityEngine;

// Central conductor: menu state, chapter start, win/lose, save (GDD sections 9/13/18).
public class GameManager : MonoBehaviour
{
    public enum GameState { Menu, Playing, Won, Dead, Paused }

    public static GameManager Instance { get; private set; }

    // MVP chapter order per GDD section 21: Medieval -> WWII -> Future.
    public static readonly WarEra[] Chapters = { WarEra.Medieval, WarEra.WorldWarTwo, WarEra.Future };

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
        Player = playerRoot.AddComponent<PlayerController>();
        Player.advancedMovement = eraManager.CanUseAdvancedMovement();
        Player.AttachCamera(cam.transform);
        cam.fieldOfView = 70f;
        PlayerHealth.onDeath += OnPlayerDeath;
        Player.weapon = CreateWeapon(era);

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

    WeaponBase CreateWeapon(WarEra era)
    {
        var weaponGo = new GameObject("Weapon");
        weaponGo.transform.SetParent(cam.transform);
        weaponGo.transform.localPosition = Vector3.zero;
        weaponGo.transform.localRotation = Quaternion.identity;

        // Ray origin sits ahead of the camera so shots clear the player's own collider.
        var firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(cam.transform);
        firePoint.localPosition = new Vector3(0f, 0f, 0.6f);
        firePoint.localRotation = Quaternion.identity;

        switch (era)
        {
            case WarEra.Medieval:
                var sword = weaponGo.AddComponent<MeleeWeapon>();
                sword.weaponName = "Longsword";
                sword.damage = 34f;
                sword.attackRate = 1.7f;
                sword.origin = cam.transform;
                return sword;
            case WarEra.WorldWarTwo:
                var rifle = weaponGo.AddComponent<FirearmWeapon>();
                rifle.weaponName = "Service Rifle";
                rifle.damage = 26f;
                rifle.attackRate = 3.2f;
                rifle.magazineSize = 8;
                rifle.ammunition = 8;
                rifle.reserveAmmo = 72;
                rifle.reloadDuration = 1.7f;
                rifle.spread = 0.012f;
                rifle.firePoint = firePoint;
                return rifle;
            default:
                var plasma = weaponGo.AddComponent<FirearmWeapon>();
                plasma.weaponName = "Plasma Rifle";
                plasma.damage = 16f;
                plasma.attackRate = 7f;
                plasma.magazineSize = 30;
                plasma.ammunition = 30;
                plasma.reserveAmmo = 180;
                plasma.reloadDuration = 1.3f;
                plasma.spread = 0.02f;
                plasma.tracerColor = new Color(0.35f, 0.95f, 1f);
                plasma.firePoint = firePoint;
                return plasma;
        }
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
        if (cam != null) cam.transform.SetParent(null);
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

using System;
using System.Collections;
using UnityEngine;
#if HAS_TIMELINE && HAS_CINEMACHINE
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;
#endif

// Cinematic cut scenes triggered by gameplay events, with letterbox bars, era
// title cards and skip (click/space/esc). Three timelines per chapter:
//   1. Chapter intro (trigger: chapter start) — 4 shots, 2 animated props
//   2. Last hostile (trigger: one enemy remaining) — dramatic push-in
//   3. Victory (trigger: level completion) — 3-shot sequence
//
// Backend: with the Timeline + Cinemachine packages present (HAS_TIMELINE /
// HAS_CINEMACHINE via asmdef versionDefines) each scene is a real
// PlayableDirector timeline built through the official Timeline scripting API —
// CinemachineTrack with overlapping virtual-camera shots (overlap = blend),
// AudioTrack with procedurally generated stings, AnimationTracks moving props.
// Without the packages everything falls back to code cinematics — never breaks.
public class CutsceneDirector : MonoBehaviour
{
    [SerializeField] float introShotDuration = 2.8f;
    [SerializeField] float shotBlendDuration = 0.8f;   // overlap between Cinemachine shots
    [SerializeField] float lastStandDuration = 1.9f;
    [SerializeField] float victoryShotDuration = 2.2f;
    [SerializeField] float victoryDuration = 3.2f;     // code-orbit fallback length
    [SerializeField] float letterboxHeight = 0.12f;    // fraction of screen height
    [SerializeField] float orbitRadius = 9f;
    [SerializeField] float orbitHeight = 4.5f;

    public bool IsRunning { get; private set; }
    public float Letterbox { get; private set; }       // animated 0..letterboxHeight
    public string TitleText { get; private set; } = "";
    public string SubtitleText { get; private set; } = "";

    bool skipRequested;
    Camera cam;

    // GDD section 2 — one bloodline across the eras.
    static string StoryLine(WarEra era)
    {
        switch (era)
        {
            case WarEra.Medieval: return "1187 — A knight of the bloodline swears to defend the realm.";
            case WarEra.WorldWarOne: return "1916 — His descendant endures the mud and thunder of the trenches.";
            case WarEra.WorldWarTwo: return "1944 — The line fights on across a broken continent.";
            case WarEra.Modern: return "2024 — A special-forces heir hunts a conspiracy centuries in the making.";
            default: return "2087 — The last descendant faces the machine built from all their wars.";
        }
    }

    // Where the era's machinery landmark sits (matches LevelBuilder placements).
    static Vector3 MachineryPoint(WarEra era)
    {
        switch (era)
        {
            case WarEra.Medieval: return new Vector3(-9f, 1.6f, 2f);    // catapult
            case WarEra.WorldWarOne: return new Vector3(8f, 1.4f, 0f);  // early tank
            case WarEra.WorldWarTwo: return new Vector3(-8f, 1.5f, 1f); // tank wreck
            case WarEra.Modern: return new Vector3(9f, 1.4f, 0f);       // patrol vehicle
            default: return new Vector3(-9f, 1.8f, 1f);                 // downed mech
        }
    }

    void Update()
    {
        if (IsRunning && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
            skipRequested = true;
    }

    // --- public API (GameManager) --------------------------------------------

    public void PlayIntro(WarEra era, int chapterIndex, Camera camera, Vector3 playerSpawn, Action onDone)
    {
        cam = camera;
        TitleText = "CHAPTER " + (chapterIndex + 1) + " — " + EraManager.DisplayName(era).ToUpper();
        SubtitleText = StoryLine(era);
        StartCoroutine(RunIntro(era, playerSpawn, onDone));
    }

    public void PlayLastStand(Camera camera, Vector3 enemyPos, Action onDone)
    {
        cam = camera;
        TitleText = "LAST HOSTILE";
        SubtitleText = "";
        StartCoroutine(RunLastStand(enemyPos, onDone));
    }

    public void PlayVictory(Camera camera, Vector3 playerPos, Action onDone)
    {
        cam = camera;
        TitleText = "";
        SubtitleText = "";
        StartCoroutine(RunVictory(playerPos, onDone));
    }

    // Hard stop (level unloading) — never leaves bars or a half-flown camera.
    public void Abort()
    {
        StopAllCoroutines();
#if HAS_TIMELINE && HAS_CINEMACHINE
        CleanupTimeline();
#endif
        IsRunning = false;
        skipRequested = false;
        Letterbox = 0f;
        TitleText = "";
        SubtitleText = "";
    }

    // --- scenes --------------------------------------------------------------

    IEnumerator RunIntro(WarEra era, Vector3 playerSpawn, Action onDone)
    {
        Begin();
        float half = LevelBuilder.ArenaHalf;
        Vector3 poi = MachineryPoint(era);
        Vector3 machineryShotPos = poi + new Vector3(poi.x < 0f ? 4.5f : -4.5f, 1.4f, -4.5f);

#if HAS_TIMELINE && HAS_CINEMACHINE
        var shots = new TShot[]
        {
            new TShot { name = "CM-Intro-Establishing", pos = new Vector3(0f, 28f, -half - 20f), lookAt = Vector3.zero, fov = 50f },
            new TShot { name = "CM-Intro-Battlefield", pos = new Vector3(-half * 0.7f, 3.2f, -2f), lookAt = new Vector3(0f, 1f, 6f), fov = 50f },
            new TShot { name = "CM-Intro-Machinery", pos = machineryShotPos, lookAt = poi, fov = 42f },
            new TShot { name = "CM-Intro-Soldier", pos = playerSpawn + new Vector3(0f, 0.9f, -2.4f), lookAt = playerSpawn + new Vector3(0f, 0.65f, 10f), fov = 70f }
        };
        yield return RunTimelineScene(shots, introShotDuration, true, StingLowBrass, () => CodeIntro(playerSpawn, machineryShotPos, poi));
#else
        yield return CodeIntro(playerSpawn, machineryShotPos, poi);
#endif
        yield return End();
        if (onDone != null) onDone();
    }

    IEnumerator RunLastStand(Vector3 enemyPos, Action onDone)
    {
        Begin();
#if HAS_TIMELINE && HAS_CINEMACHINE
        var shots = new TShot[]
        {
            new TShot { name = "CM-LastStand-Far", pos = enemyPos + new Vector3(-4.5f, 3.4f, -5.5f), lookAt = enemyPos + Vector3.up * 0.8f, fov = 45f },
            new TShot { name = "CM-LastStand-Close", pos = enemyPos + new Vector3(1.7f, 1.3f, -2.4f), lookAt = enemyPos + Vector3.up * 0.9f, fov = 38f }
        };
        yield return RunTimelineScene(shots, lastStandDuration * 0.5f, false, StingTense, () => CodeLastStand(enemyPos));
#else
        yield return CodeLastStand(enemyPos);
#endif
        yield return End();
        if (onDone != null) onDone();
    }

    IEnumerator RunVictory(Vector3 playerPos, Action onDone)
    {
        Begin();
#if HAS_TIMELINE && HAS_CINEMACHINE
        var shots = new TShot[]
        {
            new TShot { name = "CM-Victory-Hero", pos = playerPos + new Vector3(-2.2f, 1.4f, 2.8f), lookAt = playerPos + Vector3.up * 1.2f, fov = 45f },
            new TShot { name = "CM-Victory-Rising", pos = playerPos + new Vector3(4f, 5f, -6f), lookAt = playerPos + Vector3.up * 1f, fov = 55f },
            new TShot { name = "CM-Victory-Sky", pos = playerPos + new Vector3(0f, 16f, -4f), lookAt = playerPos, fov = 60f }
        };
        yield return RunTimelineScene(shots, victoryShotDuration, false, StingVictory, () => CodeOrbit(playerPos));
#else
        yield return CodeOrbit(playerPos);
#endif
        yield return End();
        if (onDone != null) onDone();
    }

    // --- code-cinematic fallbacks --------------------------------------------

    IEnumerator CodeIntro(Vector3 playerSpawn, Vector3 machineryShotPos, Vector3 poi)
    {
        float half = LevelBuilder.ArenaHalf;
        yield return Shot(new Vector3(0f, 28f, -half - 20f), new Vector3(0f, 19f, -half - 7f),
                          Vector3.zero, introShotDuration);
        if (!skipRequested)
            yield return Shot(new Vector3(-half * 0.7f, 3.2f, -2f), new Vector3(half * 0.6f, 2.4f, 4f),
                              new Vector3(0f, 1f, 6f), introShotDuration);
        if (!skipRequested)
            yield return Shot(machineryShotPos + new Vector3(0f, 0.4f, -1.5f), machineryShotPos, poi, introShotDuration * 0.8f);
        if (!skipRequested)
            yield return Shot(playerSpawn + new Vector3(0f, 3f, -6f), playerSpawn + new Vector3(0f, 0.65f, 0f),
                              playerSpawn + new Vector3(0f, 0.65f, 10f), introShotDuration * 0.8f);
    }

    IEnumerator CodeLastStand(Vector3 enemyPos)
    {
        yield return Shot(enemyPos + new Vector3(-4.5f, 3.4f, -5.5f), enemyPos + new Vector3(1.7f, 1.3f, -2.4f),
                          enemyPos + Vector3.up * 0.9f, lastStandDuration);
    }

    IEnumerator CodeOrbit(Vector3 playerPos)
    {
        Vector3 pivot = playerPos + Vector3.up * 1f;
        float t = 0f;
        while (t < 1f && !skipRequested)
        {
            t += Time.deltaTime / victoryDuration;
            float eased = Ease(Mathf.Clamp01(t));
            float angle = Mathf.Lerp(0f, 4.2f, eased);          // ~240 degrees, radians
            float radius = Mathf.Lerp(orbitRadius * 0.55f, orbitRadius, eased);
            Vector3 pos = pivot + new Vector3(Mathf.Sin(angle) * radius,
                                              orbitHeight * (0.4f + 0.6f * eased),
                                              -Mathf.Cos(angle) * radius);
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(pivot - pos);
            AnimateBars(true);
            yield return null;
        }
    }

    // --- shared plumbing -----------------------------------------------------

    void Begin()
    {
        IsRunning = true;
        skipRequested = false;
        Letterbox = 0f;
    }

    IEnumerator End()
    {
        while (Letterbox > 0.001f)
        {
            AnimateBars(false);
            yield return null;
        }
        Letterbox = 0f;
        IsRunning = false;
        TitleText = "";
        SubtitleText = "";
    }

    IEnumerator Shot(Vector3 from, Vector3 to, Vector3 lookAt, float duration)
    {
        float t = 0f;
        while (t < 1f && !skipRequested)
        {
            t += Time.deltaTime / duration;
            float eased = Ease(Mathf.Clamp01(t));
            Vector3 pos = Vector3.Lerp(from, to, eased);
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(lookAt - pos);
            AnimateBars(true);
            yield return null;
        }
    }

    void AnimateBars(bool show)
    {
        float target = show ? letterboxHeight : 0f;
        float speed = letterboxHeight / 0.35f;   // bars slide over ~0.35s
        Letterbox = Mathf.MoveTowards(Letterbox, target, speed * Time.deltaTime);
    }

    static float Ease(float t) { return t * t * (3f - 2f * t); }  // smoothstep

#if HAS_TIMELINE && HAS_CINEMACHINE
    // ---- Timeline + Cinemachine backend -------------------------------------

    struct TShot { public string name; public Vector3 pos; public Vector3 lookAt; public float fov; }

    GameObject rig;
    PlayableDirector director;
    CinemachineBrain brain;

    // Builds and plays one timeline; on any build error falls back to the
    // supplied code cinematic.
    IEnumerator RunTimelineScene(TShot[] shots, float shotDuration, bool withProps,
                                 Func<AudioClip> sting, Func<IEnumerator> fallback)
    {
        bool ok = true;
        try { BuildTimeline(shots, shotDuration, withProps, sting); }
        catch (Exception e)
        {
            Debug.LogWarning("Timeline scene unavailable, using code cinematic: " + e.Message);
            CleanupTimeline();
            ok = false;
        }
        if (ok)
        {
            double total = shots.Length * (double)shotDuration + shotBlendDuration;
            while (director != null && director.time < total - 0.05 && !skipRequested)
            {
                AnimateBars(true);
                yield return null;
            }
            CleanupTimeline();
        }
        else
            yield return fallback();
    }

    void BuildTimeline(TShot[] shots, float shotDuration, bool withProps, Func<AudioClip> sting)
    {
        float half = LevelBuilder.ArenaHalf;
        rig = new GameObject("TimelineCutscene");

        brain = cam.GetComponent<CinemachineBrain>();
        if (brain == null) brain = cam.gameObject.AddComponent<CinemachineBrain>();
        brain.enabled = true;

        director = rig.AddComponent<PlayableDirector>();
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        timeline.name = "CutsceneTimeline";

        // Camera control track: overlapping shots = smooth Cinemachine blends.
        var camTrack = timeline.CreateTrack<CinemachineTrack>(null, "Camera");
        director.SetGenericBinding(camTrack, brain);
        for (int i = 0; i < shots.Length; i++)
        {
            var vcam = MakeVcam(shots[i]);
            AddShot(camTrack, vcam, i * (double)shotDuration, shotDuration + shotBlendDuration);
        }
        double total = shots.Length * (double)shotDuration + shotBlendDuration;

        // Audio track: procedural sting (no audio assets in repo).
        try
        {
            var audioTrack = timeline.CreateTrack<AudioTrack>(null, "Audio");
            var clip = audioTrack.CreateClip<AudioPlayableAsset>();
            var stingClip = sting();
            ((AudioPlayableAsset)clip.asset).clip = stingClip;
            clip.start = 0.0;
            clip.duration = Mathf.Min((float)total, 2.6f);
            var src = rig.AddComponent<AudioSource>();
            director.SetGenericBinding(audioTrack, src);
        }
        catch (Exception e) { Debug.LogWarning("Timeline audio track skipped: " + e.Message); }

        // Animation tracks: recon drone + ground transport crossing the arena.
        if (withProps)
        {
            TryAnimatedProp(timeline, "CinematicFlyover", new Vector3(1.4f, 0.25f, 0.9f),
                new Color(0.25f, 0.26f, 0.3f),
                new Vector3(-half - 8f, 14f, 6f), new Vector3(half + 8f, 10f, -4f), (float)total);
            TryAnimatedProp(timeline, "CinematicTransport", new Vector3(2.4f, 1.1f, 1.3f),
                new Color(0.3f, 0.3f, 0.24f),
                new Vector3(-half - 6f, 0.55f, 9f), new Vector3(half + 6f, 0.55f, 9f), (float)total);
        }

        director.playableAsset = timeline;
        director.extrapolationMode = DirectorWrapMode.None;
        director.Play();
    }

    void TryAnimatedProp(TimelineAsset timeline, string name, Vector3 size, Color color,
                         Vector3 from, Vector3 to, float total)
    {
        try
        {
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = name;
            Destroy(prop.GetComponent<Collider>());
            prop.transform.SetParent(rig.transform);
            prop.transform.localScale = size;
            prop.GetComponent<Renderer>().material = LevelBuilder.ColoredMaterial(color, false);
            var animator = prop.AddComponent<Animator>();

            var moveClip = new AnimationClip();
            moveClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, from.x, total, to.x));
            moveClip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0f, from.y, total, to.y));
            moveClip.SetCurve("", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0f, from.z, total, to.z));

            var animTrack = timeline.CreateTrack<AnimationTrack>(null, name);
            director.SetGenericBinding(animTrack, animator);
            var aClip = animTrack.CreateClip<AnimationPlayableAsset>();
            ((AnimationPlayableAsset)aClip.asset).clip = moveClip;
            aClip.start = 0.0;
            aClip.duration = total;
        }
        catch (Exception e) { Debug.LogWarning("Timeline animation track skipped: " + e.Message); }
    }

    void AddShot(CinemachineTrack track, CinemachineVirtualCameraBase vcam, double start, double duration)
    {
        var clip = track.CreateClip<CinemachineShot>();
        clip.start = start;
        clip.duration = duration;
        clip.displayName = vcam.name;
        var shot = (CinemachineShot)clip.asset;
        shot.VirtualCamera.exposedName = vcam.name;
        director.SetReferenceValue(shot.VirtualCamera.exposedName, vcam);
    }

    CinemachineCamera MakeVcam(TShot s)
    {
        var go = new GameObject(s.name);
        go.transform.SetParent(rig.transform);
        go.transform.position = s.pos;
        go.transform.rotation = Quaternion.LookRotation(s.lookAt - s.pos);
        var vc = go.AddComponent<CinemachineCamera>();
        vc.Lens.FieldOfView = s.fov;
        return vc;
    }

    // Procedural stings built from sine partials — keeps the repo asset-free.
    AudioClip StingLowBrass() { return MakeSting(new[] { 110f, 220f, 277f }, 2.4f, 1.7f); }
    AudioClip StingTense() { return MakeSting(new[] { 98f, 104f, 196f }, 1.2f, 2.6f); }
    AudioClip StingVictory() { return MakeSting(new[] { 220f, 277f, 330f, 440f }, 2.4f, 1.4f); }

    AudioClip MakeSting(float[] freqs, float dur, float decay)
    {
        int rate = 44100;
        int n = (int)(rate * dur);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Exp(-decay * t);
            float s = 0f;
            for (int f = 0; f < freqs.Length; f++)
                s += Mathf.Sin(2f * Mathf.PI * freqs[f] * t) / (1f + f);
            data[i] = s * env * 0.2f;
        }
        var clip = AudioClip.Create("Sting", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void CleanupTimeline()
    {
        if (director != null) director.Stop();
        if (rig != null) Destroy(rig);
        rig = null;
        director = null;
        // With no live virtual cameras the brain is inert, but disable it so
        // gameplay camera control is untouched between cut scenes.
        if (brain != null) brain.enabled = false;
    }
#endif
}

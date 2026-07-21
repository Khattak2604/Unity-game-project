using System;
using System.Collections;
using UnityEngine;
#if HAS_TIMELINE && HAS_CINEMACHINE
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;
#endif

// Cinematic cut scenes triggered by gameplay events (chapter start, level
// completion), with letterbox bars, era title cards and skip (click/space/esc).
//
// Intro backend: when the Timeline + Cinemachine packages are present
// (HAS_TIMELINE/HAS_CINEMACHINE via asmdef versionDefines), the intro is a real
// PlayableDirector timeline built through the official Timeline scripting API —
// a CinemachineTrack with three overlapping virtual-camera shots (overlap =
// smooth blend), an AudioTrack with a cinematic sting, and an AnimationTrack
// flying a recon prop over the field. If the packages are missing the code
// falls back to plain camera lerps, so the game can never break.
public class CutsceneDirector : MonoBehaviour
{
    [SerializeField] float introShotDuration = 2.4f;
    [SerializeField] float shotBlendDuration = 0.8f;   // overlap between Cinemachine shots
    [SerializeField] float victoryDuration = 3.2f;
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

    void Update()
    {
        if (IsRunning && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
            skipRequested = true;
    }

    public void PlayIntro(WarEra era, int chapterIndex, Camera camera, Vector3 playerSpawn, Action onDone)
    {
        cam = camera;
        TitleText = "CHAPTER " + (chapterIndex + 1) + " — " + EraManager.DisplayName(era).ToUpper();
        SubtitleText = StoryLine(era);
        StartCoroutine(RunIntro(playerSpawn, onDone));
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

    IEnumerator RunIntro(Vector3 playerSpawn, Action onDone)
    {
        Begin();
#if HAS_TIMELINE && HAS_CINEMACHINE
        bool timelineOk = true;
        try { BuildIntroTimeline(playerSpawn); }
        catch (Exception e)
        {
            Debug.LogWarning("Timeline intro unavailable, using code cinematic: " + e.Message);
            CleanupTimeline();
            timelineOk = false;
        }
        if (timelineOk)
        {
            double total = introShotDuration * 3f + shotBlendDuration;
            while (director != null && director.time < total - 0.05 && !skipRequested)
            {
                AnimateBars(true);
                yield return null;
            }
            CleanupTimeline();
        }
        else
            yield return CodeIntro(playerSpawn);
#else
        yield return CodeIntro(playerSpawn);
#endif
        yield return End();
        if (onDone != null) onDone();
    }

    IEnumerator CodeIntro(Vector3 playerSpawn)
    {
        float half = LevelBuilder.ArenaHalf;
        yield return Shot(new Vector3(0f, 26f, -half - 18f), new Vector3(0f, 19f, -half - 7f),
                          Vector3.zero, introShotDuration);
        if (!skipRequested)
            yield return Shot(new Vector3(-half * 0.7f, 3.2f, -2f), new Vector3(half * 0.6f, 2.4f, 4f),
                              new Vector3(0f, 1f, 6f), introShotDuration);
        if (!skipRequested)
            yield return Shot(playerSpawn + new Vector3(0f, 3f, -6f), playerSpawn + new Vector3(0f, 0.65f, 0f),
                              playerSpawn + new Vector3(0f, 0.65f, 10f), introShotDuration * 0.8f);
    }

    IEnumerator RunVictory(Vector3 playerPos, Action onDone)
    {
        Begin();
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
        yield return End();
        if (onDone != null) onDone();
    }

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

    GameObject rig;
    PlayableDirector director;
    CinemachineBrain brain;

    void BuildIntroTimeline(Vector3 playerSpawn)
    {
        float half = LevelBuilder.ArenaHalf;
        rig = new GameObject("TimelineCutscene");

        brain = cam.GetComponent<CinemachineBrain>();
        if (brain == null) brain = cam.gameObject.AddComponent<CinemachineBrain>();
        brain.enabled = true;

        var vcamA = MakeVcam("CM-Intro-Establishing", new Vector3(0f, 26f, -half - 18f), Vector3.zero, 55f);
        var vcamB = MakeVcam("CM-Intro-Battlefield", new Vector3(-half * 0.7f, 3.2f, -2f), new Vector3(0f, 1f, 6f), 50f);
        var vcamC = MakeVcam("CM-Intro-Soldier", playerSpawn + new Vector3(0f, 0.9f, -2.4f),
                             playerSpawn + new Vector3(0f, 0.65f, 10f), 70f);

        director = rig.AddComponent<PlayableDirector>();
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        timeline.name = "IntroTimeline";

        // Camera control track: overlapping shots = smooth Cinemachine blends.
        var camTrack = timeline.CreateTrack<CinemachineTrack>(null, "Camera");
        director.SetGenericBinding(camTrack, brain);
        double d = introShotDuration, blend = shotBlendDuration;
        AddShot(camTrack, vcamA, 0.0, d + blend);
        AddShot(camTrack, vcamB, d, d + blend);
        AddShot(camTrack, vcamC, d * 2.0, d + blend);

        // Audio track: procedural cinematic sting (no audio assets in repo).
        try
        {
            var audioTrack = timeline.CreateTrack<AudioTrack>(null, "Audio");
            var clip = audioTrack.CreateClip<AudioPlayableAsset>();
            ((AudioPlayableAsset)clip.asset).clip = CinematicSting();
            clip.start = 0.0;
            clip.duration = 2.4;
            var src = rig.AddComponent<AudioSource>();
            director.SetGenericBinding(audioTrack, src);
        }
        catch (Exception e) { Debug.LogWarning("Timeline audio track skipped: " + e.Message); }

        // Animation track: recon drone prop crossing the sky during the intro.
        try
        {
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = "CinematicFlyover";
            Destroy(prop.GetComponent<Collider>());
            prop.transform.SetParent(rig.transform);
            prop.transform.localScale = new Vector3(1.4f, 0.25f, 0.9f);
            prop.GetComponent<Renderer>().material =
                LevelBuilder.ColoredMaterial(new Color(0.25f, 0.26f, 0.3f), false);
            var animator = prop.AddComponent<Animator>();

            var flyClip = new AnimationClip();
            float total = (float)(introShotDuration * 3f + shotBlendDuration);
            flyClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, -half - 8f, total, half + 8f));
            flyClip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0f, 14f, total, 10f));
            flyClip.SetCurve("", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0f, 6f, total, -4f));

            var animTrack = timeline.CreateTrack<AnimationTrack>(null, "Animation");
            director.SetGenericBinding(animTrack, animator);
            var aClip = animTrack.CreateClip<AnimationPlayableAsset>();
            ((AnimationPlayableAsset)aClip.asset).clip = flyClip;
            aClip.start = 0.0;
            aClip.duration = total;
        }
        catch (Exception e) { Debug.LogWarning("Timeline animation track skipped: " + e.Message); }

        director.playableAsset = timeline;
        director.extrapolationMode = DirectorWrapMode.None;
        director.Play();
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

    CinemachineCamera MakeVcam(string name, Vector3 pos, Vector3 lookAt, float fov)
    {
        var go = new GameObject(name);
        go.transform.SetParent(rig.transform);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(lookAt - pos);
        var vc = go.AddComponent<CinemachineCamera>();
        vc.Lens.FieldOfView = fov;
        return vc;
    }

    // Low brass-style hit built from sine partials — keeps the repo asset-free.
    AudioClip CinematicSting()
    {
        int rate = 44100;
        float dur = 2.2f;
        int n = (int)(rate * dur);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Exp(-1.7f * t);
            data[i] = (Mathf.Sin(2f * Mathf.PI * 110f * t)
                     + 0.5f * Mathf.Sin(2f * Mathf.PI * 220f * t)
                     + 0.25f * Mathf.Sin(2f * Mathf.PI * 277f * t)) * env * 0.22f;
        }
        var clip = AudioClip.Create("CinematicSting", n, 1, rate, false);
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

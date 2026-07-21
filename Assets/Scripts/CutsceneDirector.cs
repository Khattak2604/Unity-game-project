using System;
using System.Collections;
using UnityEngine;

// Runtime cinematic cut scenes: camera shot sequences with letterbox bars and
// era title cards, triggered by gameplay events (chapter start, level
// completion). Skippable with click/space/esc.
// ponytail: pure-code cinematics — the Timeline/Cinemachine authoring stack is
// editor-side content; docs/GUIDE_cutscenes_timeline.md covers that path.
public class CutsceneDirector : MonoBehaviour
{
    [SerializeField] float introShotDuration = 2.4f;
    [SerializeField] float victoryDuration = 3.2f;
    [SerializeField] float letterboxHeight = 0.12f;  // fraction of screen height
    [SerializeField] float orbitRadius = 9f;
    [SerializeField] float orbitHeight = 4.5f;

    public bool IsRunning { get; private set; }
    public float Letterbox { get; private set; }     // animated 0..letterboxHeight
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
        IsRunning = false;
        skipRequested = false;
        Letterbox = 0f;
        TitleText = "";
        SubtitleText = "";
    }

    IEnumerator RunIntro(Vector3 playerSpawn, Action onDone)
    {
        Begin();
        float half = LevelBuilder.ArenaHalf;

        // Shot 1: high establishing sweep
        yield return Shot(new Vector3(0f, 26f, -half - 18f), new Vector3(0f, 19f, -half - 7f),
                          Vector3.zero, introShotDuration);
        // Shot 2: low dolly across the battlefield
        if (!skipRequested)
            yield return Shot(new Vector3(-half * 0.7f, 3.2f, -2f), new Vector3(half * 0.6f, 2.4f, 4f),
                              new Vector3(0f, 1f, 6f), introShotDuration);
        // Shot 3: settle into the soldier's eyes
        if (!skipRequested)
            yield return Shot(playerSpawn + new Vector3(0f, 3f, -6f), playerSpawn + new Vector3(0f, 0.65f, 0f),
                              playerSpawn + new Vector3(0f, 0.65f, 10f), introShotDuration * 0.8f);

        yield return End();
        if (onDone != null) onDone();
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
}

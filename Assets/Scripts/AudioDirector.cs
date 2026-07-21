using System.Collections.Generic;
using UnityEngine;

// Three-layer audio: era background music, environmental ambience, and
// event-triggered action SFX. The repo ships no sound files — every clip is
// synthesized at load time (sine partials + shaped noise), so clone-and-run
// needs zero assets. ponytail: replace individual keys with recorded clips by
// dropping files in Assets/Audio and swapping Get() — see docs/GUIDE_audio.md.
public class AudioDirector : MonoBehaviour
{
    public static AudioDirector Instance { get; private set; }

    [SerializeField, Range(0f, 1f)] float musicVolume = 0.30f;
    [SerializeField, Range(0f, 1f)] float ambienceVolume = 0.20f;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 0.85f;

    const int Rate = 44100;

    AudioSource musicSource;
    AudioSource ambienceSource;
    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    bool warAmbience;
    float nextRumble;

    // --- static, null-safe entry points for gameplay code --------------------

    public static void SFX(string key, Vector3 pos, float vol = 1f)
    {
        if (Instance != null) Instance.PlayAt(key, pos, vol);
    }

    public static void UI(string key, float vol = 1f)
    {
        if (Instance != null) Instance.Play2D(key, vol);
    }

    // --- lifecycle -----------------------------------------------------------

    void Awake()
    {
        Instance = this;
        musicSource = NewSource("Music");
        ambienceSource = NewSource("Ambience");
    }

    AudioSource NewSource(string name)
    {
        var go = new GameObject("Audio_" + name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        return src;
    }

    void Update()
    {
        // live volume sliders
        if (musicSource != null) musicSource.volume = musicVolume;
        if (ambienceSource != null) ambienceSource.volume = ambienceVolume;

        // distant artillery in the war eras (3D, random direction)
        var gm = GameManager.Instance;
        if (warAmbience && gm != null && gm.IsPlaying && Time.time >= nextRumble)
        {
            nextRumble = Time.time + Random.Range(9f, 18f);
            Vector3 center = gm.Player != null ? gm.Player.transform.position : Vector3.zero;
            Vector3 far = center + new Vector3(Random.Range(-35f, 35f), 6f, Random.Range(25f, 45f));
            PlayAt("rumble", far, 0.55f);
        }
    }

    // --- layers --------------------------------------------------------------

    public void StartEra(WarEra era)
    {
        musicSource.clip = MusicFor(era);
        musicSource.Play();
        ambienceSource.clip = Get("wind");
        ambienceSource.Play();
        warAmbience = era == WarEra.WorldWarOne || era == WarEra.WorldWarTwo || era == WarEra.Modern;
        nextRumble = Time.time + Random.Range(4f, 9f);
    }

    public void StartMenu()
    {
        musicSource.clip = Get("music_menu");
        musicSource.Play();
        ambienceSource.Stop();
        warAmbience = false;
    }

    public void Play2D(string key, float vol = 1f)
    {
        // 2D-ish: play at the listener so there is no directional attenuation
        Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(Get(key), pos, vol * sfxVolume);
    }

    public void PlayAt(string key, Vector3 pos, float vol = 1f)
    {
        AudioSource.PlayClipAtPoint(Get(key), pos, vol * sfxVolume);
    }

    // --- synthesis -----------------------------------------------------------

    AudioClip MusicFor(WarEra era)
    {
        switch (era)
        {
            case WarEra.Medieval: return GetMusic("music_medieval", 146.8f, new[] { 1f, 1.5f, 2f }, 0f);
            case WarEra.WorldWarOne: return GetMusic("music_ww1", 98f, new[] { 1f, 1.19f, 1.5f }, 0.7f);
            case WarEra.WorldWarTwo: return GetMusic("music_ww2", 110f, new[] { 1f, 1.5f, 2f }, 1.8f);
            case WarEra.Modern: return GetMusic("music_modern", 82.4f, new[] { 1f, 1.5f, 2.99f }, 2.2f);
            default: return GetMusic("music_future", 65.4f, new[] { 1f, 2f, 2.99f, 4.76f }, 1.4f);
        }
    }

    AudioClip GetMusic(string key, float root, float[] ratios, float pulseHz)
    {
        if (cache.TryGetValue(key, out AudioClip hit)) return hit;
        var clip = MakeLoop(key, 12f, root, ratios, pulseHz);
        cache[key] = clip;
        return clip;
    }

    AudioClip Get(string key)
    {
        if (cache.TryGetValue(key, out AudioClip hit)) return hit;
        AudioClip clip;
        switch (key)
        {
            case "music_menu": clip = MakeLoop(key, 12f, 110f, new[] { 1f, 1.5f }, 0f); break;
            case "wind": clip = MakeWind(); break;
            case "rumble": clip = Tone(1.3f, 45f, 5f, 0.9f, 0.15f); break;
            case "shot_rifle": clip = Gunshot(0.35f, 130f, 30f); break;
            case "shot_pistol": clip = Gunshot(0.24f, 170f, 42f); break;
            case "shot_auto": clip = Gunshot(0.18f, 150f, 48f); break;
            case "shot_plasma": clip = Plasma(); break;
            case "bow": clip = BowRelease(); break;
            case "arrow_hit": clip = Tone(0.16f, 90f, 30f, 0.7f, 0.5f); break;
            case "whoosh": clip = Whoosh(0.28f, 0.55f); break;
            case "hit": clip = Tone(0.18f, 70f, 25f, 0.8f, 0.35f); break;
            case "death": clip = Tone(0.42f, 55f, 11f, 0.85f, 0.5f); break;
            case "hurt": clip = Tone(0.2f, 92f, 22f, 0.8f, 0.25f); break;
            case "reload": clip = Clicks(2, 0.18f); break;
            case "switch": clip = Clicks(1, 0f); break;
            case "jump": clip = Whoosh(0.18f, 0.35f); break;
            case "dash": clip = Whoosh(0.26f, 0.7f); break;
            case "step_a": clip = Step(1f); break;
            case "step_b": clip = Step(0.85f); break;
            default: clip = Clicks(1, 0f); break;
        }
        cache[key] = clip;
        return clip;
    }

    static AudioClip Bake(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // frequency snapped to a whole number of cycles per loop = seamless looping
    static float Q(float freq, float loopSeconds)
    {
        return Mathf.Max(1f, Mathf.Round(freq * loopSeconds)) / loopSeconds;
    }

    static AudioClip MakeLoop(string name, float seconds, float root, float[] ratios, float pulseHz)
    {
        int n = (int)(Rate * seconds);
        var data = new float[n];
        float swellHz = Q(0.1f, seconds);
        float pq = pulseHz > 0f ? Q(pulseHz, seconds) : 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float s = 0f;
            for (int p = 0; p < ratios.Length; p++)
                s += Mathf.Sin(2f * Mathf.PI * Q(root * ratios[p], seconds) * t) / (1.6f + p);
            float swell = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * swellHz * t);
            float v = s * swell;
            if (pq > 0f)
            {
                float beat = t * pq - Mathf.Floor(t * pq);
                v += Mathf.Sin(2f * Mathf.PI * Q(root * 0.5f, seconds) * t) * Mathf.Exp(-5f * beat) * 0.8f;
            }
            data[i] = v * 0.16f;
        }
        return Bake(name, data);
    }

    static AudioClip MakeWind()
    {
        float seconds = 10f;
        int n = (int)(Rate * seconds);
        var data = new float[n];
        var rng = new System.Random(7);
        float v = 0f, peak = 0.001f;
        for (int i = 0; i < n; i++)
        {
            v = v * 0.996f + ((float)rng.NextDouble() * 2f - 1f) * 0.05f;   // brown-ish noise
            data[i] = v;
            if (Mathf.Abs(v) > peak) peak = Mathf.Abs(v);
        }
        float swellA = Q(0.13f, seconds), swellB = Q(0.31f, seconds);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float swell = 0.6f + 0.25f * Mathf.Sin(2f * Mathf.PI * swellA * t)
                               + 0.15f * Mathf.Sin(2f * Mathf.PI * swellB * t);
            data[i] = data[i] / peak * swell * 0.5f;
        }
        return Bake("wind", data);
    }

    static AudioClip Gunshot(float dur, float boomFreq, float decay)
    {
        int n = (int)(Rate * dur);
        var data = new float[n];
        var rng = new System.Random(11);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float crack = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-decay * 1.6f * t);
            float boom = Mathf.Sin(2f * Mathf.PI * boomFreq * t) * Mathf.Exp(-decay * 0.6f * t);
            data[i] = (crack * 0.75f + boom * 0.6f) * 0.8f;
        }
        return Bake("gunshot", data);
    }

    static AudioClip Plasma()
    {
        float dur = 0.3f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float f = 900f - 700f * (t / dur);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-13f * t) * 0.6f;
        }
        return Bake("plasma", data);
    }

    static AudioClip BowRelease()
    {
        float dur = 0.25f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        var rng = new System.Random(3);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float f = 220f - 110f * (t / dur);
            float pluck = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-24f * t);
            float snap = i < Rate / 300 ? ((float)rng.NextDouble() * 2f - 1f) * 0.5f : 0f;
            data[i] = (pluck * 0.7f + snap) * 0.8f;
        }
        return Bake("bow", data);
    }

    static AudioClip Whoosh(float dur, float amp)
    {
        int n = (int)(Rate * dur);
        var data = new float[n];
        var rng = new System.Random(5);
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Sin(Mathf.PI * t / dur);
            float white = (float)rng.NextDouble() * 2f - 1f;
            prev = prev * 0.86f + white * 0.14f;   // crude low-pass = airy swish
            data[i] = prev * env * env * amp * 2.2f;
        }
        return Bake("whoosh", data);
    }

    static AudioClip Tone(float dur, float freq, float decay, float amp, float noiseMix)
    {
        int n = (int)(Rate * dur);
        var data = new float[n];
        var rng = new System.Random(9);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Exp(-decay * t);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-decay * 3f * t);
            data[i] = (tone * (1f - noiseMix) + noise * noiseMix) * env * amp;
        }
        return Bake("tone", data);
    }

    static AudioClip Clicks(int count, float gap)
    {
        float dur = 0.06f + gap * (count - 1) + 0.06f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        var rng = new System.Random(13);
        for (int c = 0; c < count; c++)
        {
            int start = (int)(Rate * gap * c);
            int len = Rate / 250;
            for (int i = 0; i < len && start + i < n; i++)
                data[start + i] = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-i / (float)len * 6f) * 0.5f;
        }
        return Bake("click", data);
    }

    static AudioClip Step(float pitchScale)
    {
        float dur = 0.07f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        var rng = new System.Random(pitchScale > 0.9f ? 17 : 19);
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float white = (float)rng.NextDouble() * 2f - 1f;
            prev = prev * 0.7f + white * 0.3f;
            data[i] = prev * Mathf.Exp(-55f * t) * 0.4f * pitchScale;
        }
        return Bake("step", data);
    }
}

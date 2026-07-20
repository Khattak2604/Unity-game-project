# Guide — Layered Audio (for this project)

Three layers: background music, action SFX, environmental ambience. You build this in the
editor; post errors/screenshots in the Teams channel for review. Estimated time: 1–2 hours.

## 1. Get free CC0 sounds (no attribution needed)

- **kenney.nl → Assets → Audio** ("Impact Sounds", "Sci-Fi Sounds", "RPG Audio", "Music Jingles")
- **freesound.org** (filter license: CC0) — search "wind loop", "medieval ambience", "gunshot",
  "bow release", "sword swing", "footsteps dirt"
- Aim for: 1 music loop per era (or one shared), 1 wind/ambience loop, gunshot, bow release,
  sword whoosh, hit/impact, reload, jump, dash, objective-complete sting.

Drop the files into `Assets/Audio/Music`, `Assets/Audio/SFX`, `Assets/Audio/Ambience`.
On each clip's import settings: music/ambience → **Streaming**; short SFX → *Decompress on Load*.

## 2. The three layers

Create `Assets/Scripts/AudioManager.cs` (yours to write — skeleton):

```csharp
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [SerializeField] AudioSource musicSource;     // loop = ON, 2D (spatialBlend 0)
    [SerializeField] AudioSource ambienceSource;  // loop = ON, 2D, quieter
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 0.9f;

    void Awake() { Instance = this; }

    public void PlayMusic(AudioClip clip) { musicSource.clip = clip; musicSource.volume = musicVolume; musicSource.Play(); }
    public void PlayAmbience(AudioClip clip) { ambienceSource.clip = clip; ambienceSource.Play(); }

    // 3D one-shot at a position (spatial audio requirement)
    public void PlayAt(AudioClip clip, Vector3 pos, float vol = 1f)
    { AudioSource.PlayClipAtPoint(clip, pos, vol * sfxVolume); }
}
```

Add two AudioSource components to a `GameObject → Create Empty` named `AudioManager` in
`Main.unity`, assign them in the Inspector.

## 3. Trigger points — already in this codebase

| Sound | Hook — call `AudioManager.Instance.PlayAt(...)` from |
|---|---|
| Gunshot / plasma | `FirearmWeapon.UseWeapon()` (after the tracer spawn) |
| Bow release | `ProjectileWeapon.UseWeapon()` |
| Sword/knife swing | `MeleeWeapon.UseWeapon()` |
| Reload | `FirearmWeapon.StartReload()` |
| Hit / hurt | `Health.TakeDamage()` (or subscribe to `onDamaged`) |
| Enemy death | `EnemyAI.OnDeath()` |
| Jump / dash | `PlayerController.Move()` — the jump and dash branches |
| Footsteps | `PlayerController` — when `IsMoving`, on a repeating timer synced to the head-bob phase |
| Objective complete | `GameManager.OnObjectiveComplete()` |
| Era music + ambience | `GameManager.StartChapter()` — pick clips per `era`, e.g. a `[SerializeField] AudioClip[] eraMusic` array |

## 4. Balance + spatial (assignment requirements)

- Music ~0.3–0.4 volume, ambience ~0.2, SFX 0.8–1.0 — tune with the [SerializeField] sliders.
- `PlayClipAtPoint` gives 3D positional SFX (enemy shots audibly left/right = spatial audio).
- Optional polish: `Edit → Project Settings → Audio` → check *Doppler*, or add an Audio Mixer
  with Music/SFX/Ambience groups and expose volumes.

## 5. Verify

Play each era: music loops, wind ambience underneath, every action in the table above makes
its sound, enemy gunfire is positional. Record a clip with `Cmd+Shift+5`.

## Reflection (if you ever need to write one)

Write it yourself from what you actually observed while balancing — which layer masked which,
what volume ratios felt right, what spatial audio changed about locating enemies. That
observation IS the content.

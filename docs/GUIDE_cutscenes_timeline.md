# Guide — Cut Scenes with Unity Timeline + Cinemachine (for this project)

You build this in the Unity editor; ask in the Teams channel when anything errors — code review
and debugging help every round. Estimated time: 1.5–3 hours.

## 1. Install the packages

1. `Window → Package Manager` → Packages: **Unity Registry**.
2. Install **Timeline** (usually pre-installed — check under *In Project*).
3. Install **Cinemachine** (v3.x on Unity 6).

## 2. Create the cutscene object

1. Open `Assets/Scenes/Main.unity`.
2. `GameObject → Create Empty` → name it `IntroCutscene`.
3. With it selected: `Window → Sequencing → Timeline` → **Create** → save as
   `Assets/Timelines/IntroCutscene.playable`. This adds a **PlayableDirector** component.
4. On the PlayableDirector: **Play On Awake = OFF** (the game triggers it, not scene load).

## 3. Cinemachine cameras

1. `GameObject → Cinemachine → Cinemachine Camera` ×2 — name them `CutCamA`, `CutCamB`.
2. Frame them on the arena (e.g. A: high wide shot at `(0, 18, -30)` looking at the field;
   B: low close shot near the player spawn `(2, 2, -14)`).
3. Add a **Cinemachine Brain** to the Main Camera (added automatically with the first CM camera).
4. In the Timeline window: `Add Track → Cinemachine Track`, bind it to the Main Camera's Brain,
   then drag `CutCamA` and `CutCamB` in as clips. **Overlap the two clips** — the overlap becomes
   a smooth blend (that is the "smooth transitions" requirement).

## 4. Animation + activation tracks

- `Add Track → Animation Track`: animate a knight/soldier walking in (record position keyframes
  on an empty GameObject or an `EnemyVisual` humanoid spawned as a prop).
- `Add Track → Activation Track`: toggle objects on/off during the scene (e.g. show a title board).
- Audio Track: drop any AudioClip (see the audio guide) for the cinematic music sting.

## 5. Trigger it from gameplay (hook points already in the code)

Create `Assets/Scripts/CutsceneTrigger.cs` — this is the part you write; skeleton:

```csharp
using UnityEngine;
using UnityEngine.Playables;

public class CutsceneTrigger : MonoBehaviour
{
    [SerializeField] PlayableDirector director;     // assign in Inspector
    [SerializeField] float startDelay = 0.5f;       // tunable per the assignment spec
    // Called by your hook — see below
    public void Play() { Invoke(nameof(Begin), startDelay); }
    void Begin() { director.Play(); }
}
```

Existing hook points in this project you can call `Play()` from:

| Event | Where in code |
|---|---|
| Game start / menu → chapter | `GameManager.StartChapter(...)` — call your trigger at the top |
| Chapter completed | `GameManager.OnObjectiveComplete()` |
| Player death | `GameManager.OnPlayerDeath()` |
| Enemy killed | `EnemyAI.OnDeath()` |

Pause gameplay during the scene: set `Time.timeScale = 0` won't advance a Timeline set to
*Game Time* — either use `DirectorUpdateMode.UnscaledGameTime` on the PlayableDirector, or
add a `Cutscene` state to `GameManager.GameState` and gate `PlayerController`/`EnemyAI`
updates on it (they already check `GameManager.Instance.IsPlaying`).

## 6. [SerializeField] tunables (assignment requirement)

Expose at minimum: `startDelay`, camera blend time (on the Cinemachine clips), scene duration.
Anything you want to tweak without re-opening Timeline.

## 7. Verify

- Enter Play mode → cutscene fires on your chosen trigger, cameras blend A→B, gameplay
  resumes after. Record with macOS `Cmd+Shift+5` screen recording.

## 8. Bonus: importing free 3D assets (10 minutes)

1. Download a CC0 pack, e.g. kenney.nl → "Blaster Kit" / "Weapon Pack" (FBX/OBJ included).
2. Drag the model files into `Assets/Models/` in the Project window — Unity imports them.
3. Drag a model into the scene to check scale; set Import Settings → Scale Factor if tiny/huge.
4. To use one as a weapon viewmodel: drop it under the Main Camera at roughly
   `(0.26, -0.24, 0.45)`, or replace the primitive parts in `WeaponViewModel.Create()` with
   `Instantiate(Resources.Load<GameObject>("Models/yourgun"))` after moving the model file to
   `Assets/Resources/Models/`.

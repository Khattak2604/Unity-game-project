# Asset workflow — you download, Claude configures

All packs below are **CC0 (free, no attribution, commercial OK)**. Download, drop the files in
the folders listed, commit + push, and post "assets pushed" in the Teams channel — the wiring
code (loading them into viewmodels / enemies / arenas, with primitive fallback if a file is
missing) gets configured for you after that.

## 1. Packs to grab

| Era / use | Pack | Link | Take these files |
|---|---|---|---|
| Future guns | Kenney — Blaster Kit | https://kenney.nl/assets/blaster-kit | `Models/GLB format/*.glb` |
| Medieval sword/bow + WWI–Modern guns | Quaternius — Weapons pack(s) | https://quaternius.com/packs.html (Weapons / Modular Weapons) | `.gltf`/`.glb` or `.fbx` |
| Humanoid soldiers | Kenney — Blocky Characters | https://kenney.nl/assets/blocky-characters | skinned `.glb` models |
| Medieval arena dressing | Kenney — Castle Kit | https://kenney.nl/assets/castle-kit | wall/tower `.glb` pieces |
| Audio (for the audio guide) | Kenney — Impact Sounds, Music Jingles, Sci-Fi Sounds | https://kenney.nl/assets?q=audio | `.ogg` files |

Any other CC0 pack you like works too — the folder convention below is what matters.

## 2. Where to put the files (exact paths)

```
Assets/Resources/Models/Weapons/medieval_sword.glb
Assets/Resources/Models/Weapons/medieval_bow.glb
Assets/Resources/Models/Weapons/ww1_rifle.glb
Assets/Resources/Models/Weapons/ww2_rifle.glb
Assets/Resources/Models/Weapons/ww2_pistol.glb
Assets/Resources/Models/Weapons/modern_rifle.glb
Assets/Resources/Models/Weapons/future_rifle.glb
Assets/Resources/Models/Weapons/future_blade.glb
Assets/Resources/Models/Characters/<era>_soldier.glb   (optional)
Assets/Audio/Music/  Assets/Audio/SFX/  Assets/Audio/Ambience/   (see GUIDE_audio.md)
```

Rename the pack files to those names (pick whichever model from the pack fits each slot —
your call which blaster becomes `future_rifle`). Unity imports on its own when you focus the
editor; let it finish, then commit **including the generated `.meta` files** and push to `main`.

## 3. What happens next

Post "assets pushed" in the channel. The wiring update then:
- replaces each primitive viewmodel with `Resources.Load<GameObject>("Models/Weapons/<name>")`
  when the file exists (primitive stays as automatic fallback);
- scales/orients the loaded model into the hand position — if a model comes in sideways or
  giant, that gets fixed per-model in code from your screenshot feedback;
- same pattern for soldier models if you add `Characters/`.

## 4. Quick sanity check on your side (2 minutes)

After Unity imports: drag one `.glb` into the scene — if it appears and isn't pink, it's good
(delete it again). Pink = missing material conversion; say so in the channel and the wiring
will handle material assignment in code.

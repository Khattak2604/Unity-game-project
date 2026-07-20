# Evolution of War

Single-player historical shooter — one bloodline fighting through five centuries of warfare.
Built from the *Evolution of War — Complete Unity Game Design Document*; this repo implements
the **MVP scope defined in section 21** of that document.

## Requirements

- **Unity 6.4.0f1** (6000.4.0f1) via Unity Hub
- Tested target: **macOS** (runs anywhere the editor runs — no platform-specific code)

## Run it

1. Clone this repo.
2. Unity Hub → **Add** → select the cloned folder → open with **6000.4.0f1**.
3. First open takes a few minutes (package import + script compile).
4. Open `Assets/Scenes/Main.unity` and press **Play**.

No extra packages, assets or setup — everything (arenas, enemies, materials, UI) is generated
procedurally at runtime from primitives.

## Controls

| Input | Action |
|---|---|
| WASD / Mouse | Move / look |
| LMB | Attack (swing / fire / loose arrow) |
| RMB (hold) | Block — melee weapons, absorbs 80% damage |
| 1–2 / scroll | Switch weapon |
| R | Reload |
| Space | Jump — **double jump in Future era** |
| Q | Dash — Future era only |
| Left Shift | Sprint |
| Esc | Pause |

## What's playable

All **five campaign chapters** (GDD §22 order), unlocked in sequence. Each era: themed arena,
two-weapon loadout with first-person viewmodels, humanoid enemy soldiers, one
*Eliminate all hostiles* objective.

| Chapter | Loadout | Arena | Enemy |
|---|---|---|---|
| Medieval | Longsword + **War Bow** (real arrows, arc + drop, they stick) | castle towers, crenellated walls | swordsman rushers |
| World War I | Bolt-action rifle + trench knife | trench lines, shell craters, heavy fog | trench riflemen (slow, hard-hitting) |
| World War II | Service rifle + sidearm | sandbags, ruined walls | riflemen, keep distance + strafe |
| Modern | **Full-auto** assault carbine + combat knife | city blocks, containers | tougher soldiers, faster fire |
| Future | Plasma rifle (auto) + energy blade | neon pylons, emissive cover | combat bots that dash |

Plus: era machinery landmarks (siege catapult, WWI early tank, knocked-out WWII tank, armored
patrol vehicle, downed combat mech), detailed weapon models (stocks, sights, magazines, bolt
handles, nocked arrow on the bow), recoil/swing animations, muzzle flash, head bob, walking limb
animation on enemies, damage flash, state-machine AI (GDD §12), health/ammo, JSON save (GDD §18).

**Guides** for editor-side work (Timeline/Cinemachine cut scenes, layered audio, asset import)
live in [`docs/`](docs/).

## Architecture (maps to the GDD)

| GDD section | Code |
|---|---|
| §9 Shared player systems | `PlayerController`, `EraManager`, `WarEra` |
| §11 Weapon system | `WeaponBase` → `FirearmWeapon` / `MeleeWeapon` / `ProjectileWeapon` (+`Arrow`, `WeaponViewModel`) |
| §12 Enemy AI (FSM) | `EnemyAI` (`EnemyState`) + `EnemyVisual` (procedural humanoid) |
| §13 Mission system | `MissionObjective` → `EliminateTargetsObjective` |
| §14 Health & damage | `IDamageable`, `Health` |
| §18 Save system | `SaveSystem`, `SaveData` |
| Conductor / arenas / UI | `GameManager`, `LevelBuilder`, `GameHUD`, `Tracer` |

Save file: `~/Library/Application Support/Neomoment/Evolution of War/evolution_of_war_save.json` (macOS).

## Deliberate MVP simplifications

- **Built-in render pipeline** instead of URP (GDD §17 recommends URP) — zero package/version risk
  for clone-and-run; URP upgrade is a clean later step.
- **Procedural primitive art** per GDD §23 ("use placeholder assets until the gameplay is fun") —
  humanoid soldiers, viewmodels and arenas are built from primitives in code. Importing modeled
  asset packs (Kenney/Quaternius/store) is the next art step and needs the Unity editor.
- **No NavMesh** — open arenas with low cover only; swap `EnemyAI` movement for `NavMeshAgent`
  when levels gain real geometry. No audio yet (polish phase, GDD §19).

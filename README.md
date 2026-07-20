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
| LMB | Attack (sword swing / fire) |
| RMB (hold) | Block — Medieval only, absorbs 80% damage |
| R | Reload |
| Space | Jump — **double jump in Future era** |
| Q | Dash — Future era only |
| Left Shift | Sprint |
| Esc | Pause |

## What's in the MVP (GDD §21)

- **Era selection menu** — full 5-chapter campaign list; 3 chapters playable, unlocked in order
- **Medieval arena** — sword melee + blocking vs swordsman rushers (GDD §4)
- **World War II arena** — service rifle vs riflemen who keep distance and strafe (GDD §6)
- **Future arena** — plasma rifle, double jump + dash vs dashing combat bots (GDD §8)
- **3 enemy types** with state-machine AI: Idle → Chase → Attack → Dead (GDD §12)
- **Health / ammo systems**, one *Eliminate all hostiles* objective per arena (GDD §13–14)
- **JSON save** — chapter unlocks persist between sessions (GDD §18)

## Architecture (maps to the GDD)

| GDD section | Code |
|---|---|
| §9 Shared player systems | `PlayerController`, `EraManager`, `WarEra` |
| §11 Weapon system | `WeaponBase` → `FirearmWeapon` / `MeleeWeapon` |
| §12 Enemy AI (FSM) | `EnemyAI` (`EnemyState`) |
| §13 Mission system | `MissionObjective` → `EliminateTargetsObjective` |
| §14 Health & damage | `IDamageable`, `Health` |
| §18 Save system | `SaveSystem`, `SaveData` |
| Conductor / arenas / UI | `GameManager`, `LevelBuilder`, `GameHUD`, `Tracer` |

Save file: `~/Library/Application Support/Neomoment/Evolution of War/evolution_of_war_save.json` (macOS).

## Deliberate MVP simplifications

- **Built-in render pipeline** instead of URP (GDD §17 recommends URP) — zero package/version risk
  for clone-and-run; URP upgrade is a clean later step.
- **Primitive placeholder art** per GDD §23 ("use placeholder assets until the gameplay is fun").
- **No NavMesh** — open arenas with low cover only; swap `EnemyAI` movement for `NavMeshAgent`
  when levels gain real geometry. No audio yet (polish phase, GDD §19).

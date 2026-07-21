# Containment Failure — Knowledge Graph

Reference snapshot of the codebase as of commit `02c33b4`. Read this instead of grepping
`Assets/Scripts` or `Assets/Prefabs` for "what exists" / "who depends on who" questions.
Only re-derive from source when this doc doesn't answer the question, or the question is
about a change made after the "last updated" line at the bottom.

## Game concept

You play A.D.A.M., an alien monster that breaks out of a research facility's basement lab.
Fight up through the facility floor by floor, killing security/military NPCs, until you reach
the surface and escape. Vampire Survivors with the roles inverted (player is the monster).

**Core loop:** clear a floor -> exit door -> harder floor above -> repeat -> final floor (surface) -> escape/win.

**Progression:** kills feed a level-up counter (`RunStats.TotalKills` vs `EvolutionSystem.killsPerLevel`).
Leveling up offers 3 random skill-card choices (`LevelUpUI`) that unlock or upgrade attacks.

### Combat evolution tree (design intent — see "Implemented vs planned" below)

Start: small melee slash only (M1). **Level 1's card choice is fixed (not random) and defines the run's archetype** (implemented 2026-07-21): Dash / Bigger Claw / Ranged Attack, always offered in that first choice.

- M1 branches — **mutually exclusive**, since both redefine what left-click does (`PlayerCombat`):
  - **Big claw** (melee build) — claw triples in range, damage doubles (`BiggerClaw` flag). Picking this permanently removes Ranged Attack from the card pool for the run.
  - **Energy ball shot** (ranged build) — replaces melee on M1 entirely, no ammo limit, cooldown-gated like melee (`UnlockedRanged` flag). Further evolves into multi-projectile or angled/diagonal spread (`DoubleProjectile` implemented as the ±12° double-shot). Picking this permanently removes Bigger Claw from the pool for the run.
- **Dash** (mobility build) is not exclusive with either of the above — it's offered alongside them in the level-1 choice and stacks with whichever M1 archetype is chosen.
- **Dash** unlock:
  - Evolves into a high-damage "dragon flight" dash attack (vfx).
  - Further evolves into an i-frame dash: character goes semi-transparent and is untargetable mid-dash.
- **Active abilities** (design targets, several partially implemented):
  - Energy beam ("kamehameha") — big charged beam attack. **Not yet implemented.**
  - Scream — radius AoE around the character. **Implemented** (`PlayerCombat.TryScream`, arc or full-radius via `RadiusScream`).
  - Cyclone — spinning claw attack hitting everything nearby while spinning. **Not yet implemented.**
  - Berserk state — temporary buffed state. **Not yet implemented.**

### Enemy tiers (design intent)

- Guard — baton (melee). Implemented as `Guard.prefab` / `GuardBrain`.
- Guard — pistol (ranged). Implemented as `GuardRanged.prefab` / `GuardRangedBrain`.
- Military — bazooka. **Not yet implemented.**
- Military — AK (ranged, presumably faster/more damage than guard pistol). **Not yet implemented.**
- Military — Tank — last floor (surface) boss before the win screen; destroying it triggers escape/win. **Not yet implemented.**
- Possible per-floor bosses / "break into an area with another monster or superhuman" set-piece rooms — **design idea, not implemented.**

### Win/lose conditions

- **Lose:** `PlayerHealth.Health` reaches 0 -> `Die()` coroutine -> `SceneTransition.LoadScene("LoseScreen")` (wiped, no longer a raw `SceneManager.LoadScene`).
- **Win (current implementation):** reach `FloorManager.IsFinalFloor` and touch the exit door -> `FloorExitDoor` calls `SceneTransition.LoadScene("Dev_FloorWin")` (the escape screen; scene already had a "Win UI"/"WinText" panel + `PlayAgainButton`). The old in-place `WinPanelUI.Show()` call was removed from this path — `WinPanelUI` is now effectively unused except `PauseMenu.RestartRun()` still calls `WinPanelUI.Continue()` defensively.
- The legacy `SpawnDirector` 60s-timer win path (`EndGame`/`Dev_Win` load) has been **removed** — the timer still ticks (harmless, unused) but no longer ends the round. `Dev_Win` scene is still in build settings but not reachable through normal play; only its own `RestartGame` button remains functional if opened directly.
- **Design target:** final floor is a surface assault ending in destroying a Tank, which triggers the true win/escape screen (Phase 4 — will replace "reach exit door" with "Tank destroyed" as the final-floor trigger).

### Implemented vs planned (quick filter)

| System | Status |
|---|---|
| Floor progression (10 floors, difficulty scaling) | Implemented (`FloorManager`) |
| Melee slash, big claw upgrade | Implemented (`PlayerCombat`, `EvolutionSystem.BiggerClaw`) |
| Ranged energy-ball equivalent (bullet), double-shot spread | Implemented (`PlayerCombat.TryRanged`, `Bullet`) |
| Scream AoE | Implemented (`PlayerCombat.TryScream`) |
| Dash | Implemented (`PlayerDash`) |
| Dash i-frames / transparency | Implemented as "Phase Dash" evolution (`EvolutionSystem.DashPhase`, `PlayerHealth.Invulnerable`) |
| Dash knockback/lunge | Implemented as "Lunge Dash" evolution (`EvolutionSystem.DashLunge`, `GuardMotor.ApplyKnockback`) — mutually exclusive with Phase Dash |
| Dash "dragon flight" heavy attack | Not implemented (Lunge Dash covers the knockback/damage half; no dedicated vfx/heavy-attack framing yet) |
| Damage-up cards per archetype | Implemented — Claw Sharpness / Overcharged Rounds / Piercing Scream (`EvolutionSystem.MeleeDamageBonus`/`RangedDamageBonus`/`ScreamDamageBonus`) |
| Scream VFX (cone + radius ring) | Implemented (`ScreamVfx`) |
| Double/Triple Shot, Bigger Bullets, Explosive Rounds | Implemented (`DoubleShot`->`TripleShot` chain, `BiggerBullets`, `ExplosiveRounds` — all in `EvolutionSystem`/`Bullet`) |
| Life Steal, Thorns | Implemented (`EvolutionSystem.NotifyKill`, `PlayerHealth.TriggerThorns`) |
| Ability cooldown HUD | Implemented (`AbilityBarUI`) |
| Kamehameha beam | Implemented — `PlayerCombat.TryBeam` (R key), `BeamVfx`, `EvolutionSystem.UnlockedBeam`/`BeamDamageBonus` |
| Cyclone spin attack | Implemented — `PlayerCombat.TryCyclone`/`CycloneRoutine` (E key), gated on `BiggerClaw` |
| Berserk state | Implemented — `PlayerCombat.TryBerserk`/`BerserkRoutine` (F key): reworked 2026-07-21 into a reckless 30s auto-pilot (player loses manual control; auto-chases/attacks with 3.5x damage) rather than a passive buff, see its doc entry |
| Guard (baton/melee) | Implemented (`GuardBrain`, `Guard.prefab`) |
| Guard (pistol/ranged) | Implemented (`GuardRangedBrain`, `GuardRanged.prefab`), now with a real gunshot sound |
| Military bazooka/AK enemies | Implemented — re-tuned `GuardRanged.prefab` instances, not new prefabs (see "Enemy tiers" below) |
| Tank boss / surface finale | Implemented — `SpawnDirector.SpawnBoss()` + `BossState`/`BossMarker`, gates the floor-10 exit door |
| Per-floor bosses | Not implemented |

When asked to build any "not implemented" row, treat the corresponding "implemented" analog
as the pattern to follow (e.g. a new enemy = copy the `Guard`/`GuardRanged` prefab+brain shape;
a new skill = add a `SkillId` in `EvolutionSystem` + a case in `ChooseSkill`).

## Enemy tiers (added 2026-07-21)

**Deliberate constraint: no new `.prefab` asset files.** Hand-authoring a brand-new prefab (new GUID,
component serialization blocks, sprite/collider setup) blind — without ever opening the Unity Editor to
verify it — is too risky; a malformed reference can silently corrupt a prefab in a way that's invisible
until someone opens the scene. Every tier below is `Guard.prefab`/`GuardRanged.prefab`, **re-tuned at
runtime after `Instantiate`** by `SpawnDirector`, the same "build it in code, no art dependency" approach
used all session for VFX/SFX/UI. This needed a handful of new public setters on scripts that only had
private serialized fields before (see each script's entry below).

| Tier | Base prefab | Appears | Tuning |
|---|---|---|---|
| Guard (baton) | `Guard.prefab` | floor 1+ | unchanged |
| Guard (pistol) | `GuardRanged.prefab` | floor 2+ | unchanged |
| Military AK | `GuardRanged.prefab` | floor 5+ (35% of ranged rolls) | faster fire (0.6s cd), more dmg (2), HP 4, dark olive tint |
| Military Bazooka | `GuardRanged.prefab` | floor 5+ (the other half of that 35%) | slow fire (2.5s cd), heavy dmg (3), explosive bullets, 1.8x bullet scale, slower move speed (1.5), HP 6, dark gray tint |
| **Tank (boss)** | `GuardRanged.prefab` | floor 10 only, spawned once | HP 40 (further scaled by floor 10's `DifficultyMultiplier`), slow (1.2 speed), heavy explosive attacks (dmg 4, 2.2x bullets), 1.4x visual scale, dark red tint |

Tinting goes through `GuardHealth.SetBaseColor(color)` (new), **not** a raw `SpriteRenderer.color` write —
`GuardHealth` caches its sprite's original color in `Awake()` for its hit-flash-then-revert, so writing
the tint directly to the `SpriteRenderer` after spawn would get silently erased the first time the guard
took damage (the flash would revert to the pre-tint color). `SpawnDirector.Tint()` also colors the
`WeaponIcon` child's `SpriteRenderer` the same way (no `WeaponIcon` code change needed — `SpriteRenderer.color`
is already public Unity API).

## Scene spine (as of the Phase 1 rewiring — 2026-07-21)

Unified end-to-end flow, all scene loads go through `SceneTransition.LoadScene()` (wipe transition):

```
Dev_MainMenu (build index 0, boot scene)
  -> MainMenu.PlayGame() -> "Master"
Master (real gameplay scene: CameraFollow, SpawnDirector, full HUD, FloorExitDoor)
  -> floor 1..9 exit: FloorExitDoor -> FloorManager.AdvanceFloor() + SpawnDirector.RescaleForFloor() (no scene change)
  -> floor 10 (final) exit: FloorExitDoor -> SceneTransition.LoadScene("Dev_FloorWin")
  -> player death: PlayerHealth.Die() -> SceneTransition.LoadScene("LoseScreen")
Dev_FloorWin (escape/win screen) -> PlayAgainButton.PlayAgain() -> resets FloorManager/RunStats -> "Master"
LoseScreen -> RestartGame.Restart() -> resets FloorManager/RunStats -> reloads GameManager.LastLevelIndex ("Master")
```

`RestartGame` and `PlayAgainButton` both reset `FloorManager`/`RunStats` before reloading, so every
new run starts clean at floor 1 regardless of which button was used.

**Retired/dead paths (still present in the repo, not part of the live spine):**
- `SpawnDirector`'s 60s round timer and `Dev_Win` scene — timer still ticks but no longer ends the
  round; `EndGame()`/timer-win removed. `Dev_Win.RestartGame` button still works if that scene is
  opened directly, but nothing routes there during normal play anymore.
- `SceneSwapper` — legacy raw-load trigger, unused by any tracked prefab/scene.
- `Dev_PlayerController` scene — an earlier dev/test gameplay scene, still in build settings, no
  longer a load target from `MainMenu` or `PlayAgainButton` (both now point at `Master`).

If asked to touch scene-load wiring again, check both the C# script's serialized-field *default*
and the actual value baked into the scene/prefab YAML — they can (and did) diverge silently, e.g.
`MainMenu.playSceneName` defaulted to `"Master"` in code but was serialized as `"Dev_PlayerController"`
in `Dev_MainMenu.unity` until this pass fixed it.

## Ownership (from code comments + README)

- `GuardBrain.cs` — owned by Jacob (comment in `GuardRangedBrain.cs`).
- `GuardRangedBrain.cs` — kept separate from `GuardBrain` specifically so as not to touch Jacob's file.
- `PlayerHealth.cs` — owned by Noah (comment in `FloorRunWatcher.cs`).
- `PlayerController.cs`, `SpawnDirector.cs` — headers note "written with AI assistance."
- General rule per README: one owner per script/prefab; ask before editing someone else's.
- `Assets/Scenes/Master.unity` is integration-only, edited only by Jacob.

## Static/global state (no MonoBehaviour, survives scene loads implicitly)

| Class | Holds | Reset by |
|---|---|---|
| `FloorManager` | `CurrentFloor`, `TotalFloors`=10, `DifficultyMultiplier` | `ResetRun()` — called by `FloorRunWatcher`, `PauseMenu.RestartRun`, `PlayAgainButton` |
| `RunStats` | Kill counts + attack-usage counts per `AttackType` | `ResetRun()` — same callers as above |
| `GameManager` | `LastLevelIndex` (build index of last scene) | never reset, only overwritten in `PlayerHealth.Die()` |

## Script reference (Assets/Scripts/)

### Core interfaces/enums
- **`IDamageable`** — `TakeDamage(int)`. Implemented by `GuardHealth`, `PlayerHealth`. Decouples attackers (`GuardBrain`, `Bullet`, `PlayerCombat`) from concrete health types.
- **`AttackType`** — enum `Melee | Ranged | Scream`. Used for kill/usage attribution throughout (`RunStats`, `EvolutionSystem`).

### Player systems (all live on `Player.prefab`)
- **`PlayerController`** — WASD movement via `Rigidbody2D.linearVelocity`. `moveSpeed`=6. Disabled by `PlayerHealth.Die()`.
- **`PlayerHealth`** — `IDamageable`. `maxHealth`=10. `public bool Invulnerable { get; set; }` — `TakeDamage` no-ops entirely while set; driven by `PlayerDash`'s Phase Dash i-frames and (added 2026-07-21) a 1.5s grace window after `SecondWind` triggers. `Heal(amount)` heals without touching the cap (unlike `IncreaseMaxHealth`), used by `Regeneration` and `LifeSteal`. `TakeDamage` order of operations (added 2026-07-21): flat `evolution.ArmorFlat` reduction (`Armor` card, floor of 1 dmg) is applied to the incoming amount first; then, if the hit would reduce `Health` to 0, `evolution.TryConsumeSecondWind()` is checked — if it returns true (once per run) `Health` clamps to 1 instead of dying, with a cyan `HitFlashFx` cue and the grace-invulnerability coroutine, otherwise death proceeds as normal. On every (non-invulnerable) hit: `Sfx.PlayRandom("player_hurt", 2, ...)`, a red `HitFlashFx.Spawn`, a brief red sprite flash, and `TriggerThorns()` (the `Thorns` card — a retaliation shockwave within `thornsRadius`=2.5, simplified from exact-attacker reflection since that would need an attacker reference threaded through `IDamageable`). On death: disables controller/colliders/sprites, sets `GameManager.LastLevelIndex`, waits `loseDelay`, `SceneTransition.LoadScene(loseScene)`. `ResetToStartingHealth()` is the in-place-restart hook. Polled (not eventful) by `FloorRunWatcher`.
- **`PlayerCombat`** (reworked 2026-07-21 — M1 archetype rebind; extended — shot-count fix + bullet/kill cards + cooldown-fraction getters; extended again — three new actives + remaining passives) — M1 (left-click) is the primary attack: melee claw by default, **permanently replaced by the ranged shot** once `EvolutionSystem.UnlockedRanged` is true (`Update()` branches on `RangedIsPrimary`, a public property; `TryMelee` becomes unreachable once ranged is picked). The old ammo economy was removed entirely — ranged is cooldown-only, same as melee. `TryRanged` branches on `evolution.TripleShot` (3 bullets: center + ±12°, this was the old mislabeled "Double Projectile" behavior) then `evolution.DoubleShot` (2 bullets, ±8°, no center — the real double shot) else a single bullet. `SpawnBullet` also passes `evolution.BulletScale`/`ExplosiveRounds`/pierce/`Ricochet` into `Bullet.Init`, plus an `onKill` callback (`() => evolution?.NotifyKill()`) for `LifeSteal`. `BiggerClaw` triples melee range and doubles damage; `TryMelee`/`TryScream`/`CycloneRoutine` all multiply their range and damage by `evolution.TitanSizeMultiplier`/`TitanDamageMultiplier` (1.5x/2x) if the `Titan` card is taken — extended 2026-07-21 from claw-only to all three "physical" attacks (see `EvolutionSystem`). Scream (Space) is independent of the M1 choice, gated by `UnlockedScream`, arc or full-radius via `RadiusScream`, calls `evolution?.NotifyKill()` on kills, and (if `KnockbackOnHit`/`ScreamSlow` are taken) applies `GuardMotor.ApplyKnockback`/`ApplySlow` to surviving guards it hits — melee does the same knockback check. Three more actives, all bound in `Update()` alongside Space: **`TryBeam()`** (**R**, requires `UnlockedBeam`) — instant `Physics2D.OverlapBoxAll` rectangle (length/width `beamLength`/`beamWidth`) in the aim direction, damages every guard hit, `BeamVfx.Spawn` + `Sfx.PlayRandom("beam_fire", ...)`. **`TryCyclone()`** (**E**, requires `UnlockedCyclone`, itself gated on `BiggerClaw` — the melee capstone) — `StartCoroutine(CycloneRoutine())`, ticking radial damage + a rotating `SpawnClawEffect` every `cycloneTickInterval` for `cycloneDuration`. **`TryBerserk()`** (**F**, requires `UnlockedBerserk`) — reckless auto-pilot state: `BerserkRoutine()` sets `berserkEndTime` (`evolution.BerserkDuration`, base **25s** +1s/`BerserkDurationUp` stack), tints the sprite dark red, **disables `PlayerController`** for the duration, and every frame re-targets the nearest `GuardHealth` (`FindNearestGuard()`, re-scanned every `berserkRetargetInterval`=0.3s) — driving `Rigidbody2D.linearVelocity` directly toward it (`berserkChaseSpeed`) until in range, then holding position and auto-swinging (`BerserkStrike()`, an AoE circle hit at `berserkAttackCooldown`=0.4s, not just the tracked target — "kill as many as possible") at 3.5x damage (`berserkDamageMultiplier`, via `ApplyDamageModifiers`). **Extended 2026-07-21 — reckless ability spam:** each loop iteration also calls `TryCyclone()` (if unlocked), and once a target exists, sets `berserkAimOverride` (a new `Vector2?` field `AimDirection()` checks first, so Beam/Scream aim at the tracked target instead of the unread mouse cursor) and calls `TryBeam()`/`TryScream()` (the latter gated to `dist <= screamRange*1.6`) — every `Try*` method already self-gates on its own cooldown/unlock, so calling them every frame just means "fire the instant it's ready," no separate per-ability timer needed in the loop. `PlayerCombat.Update()` suspends all manual attack input while `IsBerserking` (still allows attempting F, harmless since the cooldown is blocked); `PlayerDash` also checks `combat.IsBerserking` to suspend dashing — between the two, the player has zero manual control for the full 25s. The real cooldown (`berserkCooldown`=45s) only starts counting once the state ends (`nextBerserkTime` is held at `float.MaxValue` during the active window), not at activation, so it can't be re-triggered mid-flight. `PlayerController` and `Rigidbody2D` are looked up fresh each activation (not cached) since this is a rare, expensive-relative-to-benefit lookup. `ApplyCooldownReduction`/`ApplyDamageModifiers` (renamed from `ApplyCrit` — now also applies Berserk's flat multiplier when `Time.time < berserkEndTime`) are shared helpers used by all five attacks (melee/ranged/scream/beam/cyclone). Beam and Cyclone attribute their `RunStats` usage to `AttackType.Ranged`/`Melee` respectively rather than adding new `AttackType` values — a deliberate scope simplification. Exposes `M1CooldownFraction`/`ScreamCooldownFraction`/`BeamCooldownFraction`/`CycloneCooldownFraction`/`BerserkCooldownFraction` (0=ready, 1=just used; computed from the *actual* cooldown applied at cast time, stored separately, since Adrenaline/etc. can change it mid-run) for `AbilityBarUI`, which it bootstraps via `AbilityBarUI.EnsureInstance(this, GetComponent<PlayerDash>())` in `Awake`. `ResetCombatState()` is the restart hook (cooldowns + Berserk state + sprite tint, no ammo).
- **`PlayerDash`** (extended 2026-07-21 — Phase/Lunge branches, cooldown-fraction getter, then Berserk gating) — Shift dash, gated by `EvolutionSystem.UnlockedDash`. `Update()` also early-returns entirely if `combat.IsBerserking` (cached `PlayerCombat` ref) — the reckless Berserk auto-pilot suspends dashing along with every other manual input. `[DefaultExecutionOrder(100)]` so it overrides `PlayerController`'s velocity in the same `FixedUpdate` tick. Two mutually exclusive evolutions change dash behavior: **Phase Dash** (`evolution.DashPhase`) sets `PlayerHealth.Invulnerable = true` for the dash's duration and drops sprite alpha to 0.45, both restored when the dash ends. **Lunge Dash** (`evolution.DashLunge`) sweeps the dash path on activation via `Physics2D.CircleCastAll` (radius `lungeRadius`, distance `dashSpeed*dashDuration`), dealing `lungeDamage` (1) to every `GuardHealth` hit, knocking each back via `GuardMotor.ApplyKnockback`, and dealing the player `lungeSelfDamagePerHit` (1) per guard hit — **summed and capped at 50% of the player's HP at the moment the dash started**. Exposes `DashCooldownFraction` (same stored-actual-cooldown pattern as `PlayerCombat`) for `AbilityBarUI`.
- **`PlayerMoveSpeedBoost`** — multiplies `Rigidbody2D.linearVelocity` by `SpeedMultiplier * TitanMultiplier` (added 2026-07-21: `TitanMultiplier` is a second, separate field so the `Titan` card's permanent 0.6x slowdown combines with — rather than overwrites — Swift's stacking speed boost). Also `[DefaultExecutionOrder(100)]`.
- **`EvolutionSystem`** (reworked 2026-07-21 across several passes — fixed starter choice, dash branches, first backlog, shot-fix, three new actives + remaining backlog, and finally exp-orb XP) — the level-up brain, now 36 `SkillId`s. `Update()`'s level target is `(RunStats.TotalKills + RunStats.BonusExp) / killsPerLevel` (was `TotalKills` only) — yellow `ExpPickup` orbs are a real second XP source alongside kills, same for the `ExpBar` display. **Level 1's `OfferChoice()` is deterministic, not random**: always exactly `UnlockDash` / `BiggerClaw` / `UnlockRanged`, so every run picks a starting archetype (mobility / melee / ranged). `BiggerClaw`/`UnlockRanged` are mutually exclusive (both define M1). `DashPhase`/`DashLunge` are mutually exclusive (both require `UnlockedDash`). `DoubleShot` (requires `UnlockedRanged`) -> `TripleShot` (requires `DoubleShot`) is a real 2-then-3-bullet chain (the old "Double Projectile" actually fired 3 — renamed/split). `MeleeDamageUp`/`RangedDamageUp`/`ScreamDamageUp` (x3, +1 dmg/stack) and `BiggerBullets` (x3, +25% bullet scale/stack, widens the real hitbox) gate on their base ability. `ExplosiveRounds`/`RangedPierce`/`Ricochet` (all require `UnlockedRanged`, one-time) modify `Bullet.Init`'s explosive/pierce/ricochet params. `ScreamCooldownDown`/`DashCooldownDown` (x3, -15%/stack, gated) and universal `Adrenaline` (x3, -10%/stack, melee/ranged/scream only) combine additively in `PlayerCombat.ApplyCooldownReduction`/`PlayerDash`'s dash-cooldown calc. `Regeneration`/`LifeSteal`/`Armor`/`SecondWind`/`KnockbackOnHit` are all universal/prereq-free (`ScreamSlow` requires `UnlockedScream`) — see `PlayerHealth`/`PlayerCombat` for how each applies. `CritChance`/"Precision" (x3, +10%/stack) rolled via `RollCrit()`. **Three new active abilities**, each unlock + one damage/duration-up card: `UnlockBeam`/`BeamDamageUp` (x3, +2 dmg/stack, prereq-free — R key), `UnlockCyclone`/`CycloneDamageUp` (x3, +1 dmg/tick/stack, `UnlockCyclone` itself requires `BiggerClaw` — the melee capstone — E key), `UnlockBerserk`/`BerserkDurationUp` (x3, +1s/stack off a **25s** base, was 30s — F key). `SecondWind` additionally tracks a private one-shot `secondWindUsed` flag (not stack-based) via `TryConsumeSecondWind()`, called from `PlayerHealth`. `Titan` (requires `BiggerClaw` — same melee-capstone gate as `UnlockCyclone`, one-time): `TitanDamageMultiplier` (2x) and `TitanSizeMultiplier` (1.5x, added 2026-07-21) are read by `PlayerCombat.TryMelee`/`TryScream`/`CycloneRoutine` — **all three "physical" attacks (claw, scream, cyclone) get both the damage and reach boost**, not just claw as in the first pass. Also `-40%` move speed (sets `PlayerMoveSpeedBoost.TitanMultiplier` directly) and scales `transform.localScale` to 1.6x — applied directly in `ChooseSkill` since `EvolutionSystem` lives on the same Player GameObject its own `transform` refers to. Beam and ranged bullets are untouched by Titan (it's the melee-archetype capstone, not a ranged buff). `Dash` itself has no exclusion and stacks with either M1 archetype. From level 2 onward, `OfferChoice()` draws randomly from the remaining not-maxed/prereq-met pool. `ChooseSkill()` applies the effect. `ResetProgress()` is the restart hook — resets all 36 skills' state (including `secondWindUsed` and `transform.localScale`).
- **`FloorRunWatcher`** — polls `PlayerHealth.Health <= 0` (no death event exists) to fire `FloorManager.ResetRun()` + `RunStats.ResetRun()` once per death.

### Guard AI (shared building blocks, both on `Guard.prefab` and `GuardRanged.prefab`)
- **`GuardPerception`** — vision cone + line-of-sight with hysteresis (`detectRadius`=6, `loseRadius`=8, `viewAngle`=70°). Fires `event Action<Transform> TargetSpotted` / `event Action TargetLost`. `SetTarget()` called by `SpawnDirector` via the brain.
- **`GuardMotor`** (extended 2026-07-21 across several passes — knockback, a speed setter, then a slow effect) — `Seek(pos)` / `Stop()`, physics-based arrival deceleration (`maxSpeed`=3, `arriveRadius`=1.5), rotates body to face velocity (`turnSpeed`=540). `ApplyKnockback(velocity, duration)` overrides normal seek/stop for `duration` seconds (checked first in `FixedUpdate`), then seeking resumes automatically — used by `PlayerDash`'s Lunge Dash and (if `KnockbackOnHit` is taken) melee/scream hits. `SetMaxSpeed(speed)` lets `SpawnDirector` slow down the Bazooka/Tank tiers. `ApplySlow(multiplier, duration)` — same auto-expiring-override shape as `ApplyKnockback`, multiplies the computed seek speed while active; used by the `ScreamSlow` card. Used by both brain types.
- **`GuardBrain`** (melee, `[RequireComponent(GuardPerception, GuardMotor)]`) — Idle/Chase/Attack state machine. `attackEnterRange`=1.2 (hysteresis exit 1.8), `attackDamage`=1, `attackCooldown`=1.0. Attacks via `IDamageable.TryGetComponent`. Every attack tick (cooldown elapsed, regardless of whether a damageable target is found) plays `Sfx.PlayRandom("guard_baton_hit", 3, ...)` + `HitFlashFx.Spawn` at the target (added 2026-07-21).
- **`GuardRangedBrain`** (ranged, same shape) — keeps distance: retreats under `retreatRange`=2.5, holds/fires between `retreatRange` and `attackEnterRange`=5 (exit 6.5). Fires `bulletPrefab` (→ `Bullet.prefab`) via `Bullet.Init(dir, dmg, playerOwned=false, 0, bulletScale, bulletExplosive)`; `Fire()` also plays `Sfx.PlayRandom("guard_gun_fire", 1, ...)` + a muzzle-flash `HitFlashFx.Spawn`. `SetAttackProfile(damage, cooldown)` and `SetBulletProfile(scale, explosive)` (added 2026-07-21) let `SpawnDirector` re-tune a spawned instance into the military/boss tiers — both default to the inspector values, so an untouched `GuardRanged.prefab` spawn is unaffected.
- **`GuardHealth`** — `IDamageable`. `maxHealth`=3, `ScaleForFloor(mult)` called by `SpawnDirector` right after spawn (multiplies by `FloorManager.DifficultyMultiplier`). `SetBaseMaxHealth(hp)` (added 2026-07-21) overrides `maxHealth` directly for the military/boss tiers — called *before* `ScaleForFloor` so the floor multiplier still applies proportionally on top. `SetBaseColor(color)` (added 2026-07-21) re-tints the guard *and* updates the cached `baseColor` the hit-flash reverts to — required because a raw `SpriteRenderer.color` write after spawn would get erased by the first hit-flash otherwise (see "Enemy tiers"). On every hit: spawns a `DamageNumber` popup and flashes the `SpriteRenderer` white for 0.08s. On death: `RunStats.RegisterKill(AttackType)`, `Destroy(gameObject)`.
- **`DamageNumber`** — lightweight runtime-only floating combat text (`TextMesh`, no font/prefab dependency). `DamageNumber.Spawn(worldPos, amount)` creates a self-destructing popup that rises and fades over 0.6s. Called by `GuardHealth.TakeDamage`; not attached to any prefab.
- **`ExpOrb`** (added 2026-07-21) — the **green** "life orb": `GuardHealth`'s death branch has a 15% chance to call `ExpOrb.Spawn(position)`, dropping a small procedural green circle (same cached-sprite/no-art-asset pattern as `HitFlashFx`) with a trigger collider. Walking over it (Player tag) heals 1 HP via `PlayerHealth.Heal` and despawns with a green `HitFlashFx` flash; uncollected orbs expire after 8s. Tied to kills only — not the persistent map-wide pickup (that's `ExpPickup`, a deliberately separate class after user feedback that the two shouldn't be the same thing).
- **`ExpPickup`** (added 2026-07-21) — the **yellow** "exp orb": visually and mechanically distinct from `ExpOrb`. `SpawnDirector` maintains a persistent pool of `expPickupPoolSize` (20) instances scattered across the map (`RandomMapPosition()` — a random spawn point + scatter-radius offset, not tied to player position or enemy deaths), refilling any collected ones back up to 20 every `expPickupRefillInterval` (30s). Unlike `ExpOrb`, these **don't expire** — they sit until collected or replaced on the next refill. Picking one up (Player tag) calls `RunStats.RegisterBonusExp(1)` (not a heal) and despawns with a yellow `HitFlashFx` flash. `EvolutionSystem`'s level-up counter reads `RunStats.TotalKills + RunStats.BonusExp`, so these are a genuine second XP source, independent of the floor kill quota (which only reads `FloorKills`).
- **`HitFlashFx`** (added 2026-07-21) — procedural attack/impact flash: builds one shared soft-edged circle sprite at runtime (cached statically, no art asset) and reuses it, tinted/sized per call. `HitFlashFx.Spawn(worldPos, color, scale)`. Used for the melee guard's swing, the ranged guard's muzzle flash, and the player's hit-react.
- **`Sfx`** (added 2026-07-21) — static one-shot audio helper mirroring the `Resources.Load<Sprite>("Combat/...")` convention: `Sfx.PlayRandom(baseName, variantCount, position, volume)` loads `Resources/Audio/{baseName}_{0..variantCount-1}` (extension-agnostic — `.ogg` or `.wav` both work via `Resources.Load<AudioClip>`) and fires `AudioSource.PlayClipAtPoint`. No persistent AudioSource needed.
- **`ScreamVfx`** (added 2026-07-21) — procedural VFX for Scream: `SpawnCone(origin, aimDir, arcDegrees, range, color)` (wedge sprite, cached per rounded arc degree) and `SpawnRing(origin, range, color)` (circle sprite), both built via pixel tests at `pixelsPerUnit = size/2` so `localScale = range` gives the sprite the *actual* hitbox radius — unlike `HitFlashFx`'s hand-tuned scale constants, this one has to truthfully represent the real attack reach. Fades alpha to 0 over 0.3s, no growth animation (Scream is instant, not a telegraphed cast). Wired into `PlayerCombat.TryScream`.
- **`BeamVfx`** (added 2026-07-21) — procedural VFX for the Kamehameha Beam: a single shared 1×1 solid-white sprite (same "build once, cache statically" approach as everything else here) stretched non-uniformly (`localScale = (length, width, 1)`) and rotated to the aim direction — a beam is just a scaled rectangle, no new texture-building logic needed beyond what `HitFlashFx`/`AbilityBarUI` already established. Fades over 0.2s. Wired into `PlayerCombat.TryBeam`.
- **`AbilityBarUI`** (added 2026-07-21, extended same day — square slots, ability names, red fill, R/E/F slots) — bottom-of-screen cooldown/keybind HUD, now 6 slots: M1, Scream (Space), Dash (Shift), Beam (R), Cyclone (E), Berserk (F). Builds its own `Canvas`/`Image`/layout entirely at runtime (same technique `SceneTransition` already uses) via `EnsureInstance(playerCombat, playerDash)`, called from `PlayerCombat.Awake()` so it's always rebuilt fresh (not `DontDestroyOnLoad`) whenever a new Player exists after a scene load. Non-M1 slots are built generically through a private `Slot` class holding `Func<bool> isUnlocked` / `Func<float> cooldownFraction` / `Func<string> abilityName` delegates, iterated in `Update()` — M1 is handled separately since its icon/name swap between claw and gun. Each slot: a perfect-square (64×64) icon `Image` — both the outer `HorizontalLayoutGroup` and each slot's `VerticalLayoutGroup` have `childControlWidth`/`childControlHeight` explicitly set `false`, since Unity's layout groups default those to `true` and would otherwise override the manually-set square `sizeDelta` — a **red** (`0.8, 0.1, 0.1, 0.75`, changed from the original dark/black) `Image.Type.Filled` (`Radial360`) cooldown overlay whose `fillAmount` is the relevant `*CooldownFraction` getter (1 the instant an ability is cast, draining to 0 as it comes off cooldown — "fills up instantly then empties out"), a `TextMeshProUGUI` keybind label, and a second `TextMeshProUGUI` showing the **ability's name** — only populated once `isUnlocked()` is true (empty string otherwise), so an ability you haven't taken yet shows a dimmed icon with no name, not a misleading label. Dash's name switches between "Dash"/"Phase Dash"/"Lunge Dash" depending which evolution was taken. `PlayerCombat` exposes `BeamCooldownFraction`/`CycloneCooldownFraction`/`BerserkCooldownFraction` (same stored-actual-cooldown pattern as `M1CooldownFraction`/`ScreamCooldownFraction`) for the three new slots.
- **`WeaponIcon`** — `[RequireComponent(SpriteRenderer)]`, loads `Resources.Load<Sprite>("Combat/{resourceName}")` at `Awake`. Lets one prefab shape serve multiple guard variants (icon string differs per prefab instance).
- **`EnemyDamage`** — simple `OnCollisionEnter2D` contact-damage component. **Not present on either guard prefab** — looks like a leftover/alternate early enemy type, not currently wired into any prefab in the repo.

### Projectile
- **`Bullet`** (`[RequireComponent(Rigidbody2D)]`, on `Bullet.prefab`) — shared by player and ranged guards; faction is a bool (`isPlayerBullet`), not tag/layer. `Init(dir, dmg, playerOwned, pierceCount = 0, scale = 1f, isExplosive = false, onKillCallback = null, canRicochet = false)` sets everything, applies `scale` as `transform.localScale` (also widens the 2D collider since it scales with the transform), and self-destroys after `lifeTime`=2. **When `scale > 1.01` (i.e. `BiggerBullets` is active), the sprite swaps to a procedural circle** (`GetCircleSprite()`, same cached-texture pattern as `HitFlashFx`/`ScreamVfx`, tinted per faction) instead of the normal `Combat/bullet_player`/`bullet_guard` art — added 2026-07-21 because the source bullet sprites read as oddly stretched when scaled up non-trivially; a circle reads cleanly as "bigger" at any scale. `OnTriggerEnter2D`: a player bullet hitting a guard invokes `onKill` if lethal, calls `Explode()` (AoE via `Physics2D.OverlapCircleAll(explosionRadius=1.2)`, `explosionDamage`=1, damages every `GuardHealth` in radius) if `explosive`; if pierce is exhausted and `ricochet` is set, `TryRicochet()` (added 2026-07-21 for the `Ricochet` card) redirects the bullet's `direction` toward the nearest other guard within `ricochetRadius`=4 (once per bullet, via `hasRicocheted`) instead of destroying it — only destroys itself if pierce is exhausted *and* ricochet is off/already used/no target found. A guard bullet hitting the player (military/Tank tiers) plays the same orange `HitFlashFx` + `Sfx.PlayRandom("explosion", 3, ...)` via a shared `PlayExplosionFx()` helper if `explosive`, but skips the AoE guard-damage loop. `GuardRangedBrain.Fire()`'s original 3-arg call pattern still works (new params are optional, default to no pierce/scale-1/non-explosive/no callback/no ricochet).

### Level/spawn/floor management
- **`FloorManager`** (static) — `CurrentFloor`/`TotalFloors`=10/`IsFinalFloor`/`DifficultyMultiplier` = `1 + 0.15*(CurrentFloor-1)`. `KillQuota` = `30 + (CurrentFloor-1)*8` (bumped from `20 + (CurrentFloor-1)*5` same day — the original numbers combined with spawn pacing that hadn't caught up yet, so floors advanced before the swarm ever got dense) — the number of floor-local kills (`RunStats.FloorKills`) `SpawnDirector` waits for before auto-advancing.
- **`FloorExitDoor`** (on `FloorDoor.prefab`, reworked 2026-07-21 for the kill-quota system) — `OnTriggerEnter2D` (Player tag + 1s debounce): **only does anything on the final floor**, and even then only loads the escape scene if `BossState.Defeated` — otherwise a no-op. Floors 1-9 no longer use the door at all; floor advancement there is fully automatic via `SpawnDirector`'s kill quota (see below), so the door sitting somewhere in the arena is now vestigial for those floors.
- **`SpawnDirector`** — wave/spawn director. Spawns `guardPrefab`/`rangedGuardPrefab` (weighted by `rangedGuardChance`=0.3, gated to `FloorManager.CurrentFloor >= 2` — floor 1 is melee-only) around the player respecting min/max distance from player and other guards. Spawns `guardsPerSpawnTick` (**3**, retuned 2026-07-21, was 2) guards every `spawnInterval` (**0.6s**, was 1s) up to `maxGuardCount` (**45**, was 30), plus an `initialBurstCount` (**10**, was 6) burst on `Start()`/`ResetForNewRun()` — bumped because the kill-quota auto-advance (below) was letting floors change before the swarm ever ramped up, so fights read as "not difficult." `RescaleForFloor()` recomputes pacing from `FloorManager.DifficultyMultiplier`. `ResetForNewRun()` is the restart hook. The old 60s round timer/`EndGame` win path is inert (see "Scene spine"). Extended for enemy tiers (see "Enemy tiers" section above): from floor `militaryStartFloor` (5), a rolled ranged spawn has `militaryUpgradeChance` (35%) to become a re-tuned military variant (AK or Bazooka, 50/50); a **plain (non-military) ranged spawn now also gets a steel-blue `Tint`** (added 2026-07-21) so pistol guards read as visually distinct from melee guards even before floor 5. On `FloorManager.IsFinalFloor`, `Update()` stops all regular spawning and calls `SpawnBoss()` exactly once (guarded by `BossState.Spawned`). Kill-quota auto-advance: on non-final floors, once `RunStats.FloorKills >= FloorManager.KillQuota`, `Update()` calls `FloorManager.AdvanceFloor()` + `RunStats.ResetFloorKills()` + `RescaleForFloor()` automatically — this is the *only* way floors 1-9 advance (see `FloorExitDoor`). Existing alive guards aren't cleared on advance. **`RefillExpPickups()`** (reworked 2026-07-21 from a single-orb-near-player timer into a persistent pool) runs every `Update()` regardless of floor (before the final-floor early-return, so it works during the boss fight too): tops the tracked `activeExpPickups` list back up to `expPickupPoolSize` (20) every `expPickupRefillInterval` (30s), placing new `ExpPickup`s at `RandomMapPosition()` (a random `spawnPoints` entry + scatter-radius offset — spread across the whole arena, not centered on the player). Also called once on `Start()`/`ResetForNewRun()` (clearing any previous pickups first on restart) to seed the initial 20.
- **`BossState`** (static, mirrors `FloorManager`/`RunStats`'s plain-static pattern) — `Spawned`/`Defeated` bools, `MarkSpawned()`/`MarkDefeated()`/`Reset()`. `Reset()` is called alongside `FloorManager.ResetRun()`/`RunStats.ResetRun()` at all 4 existing restart call sites (`FloorRunWatcher`, `PauseMenu.RestartRun`, `PlayAgainButton.PlayAgain`, `RestartGame.Restart`).
- **`BossMarker`** — attached to the Tank instance alongside its untouched `GuardHealth`; `OnDestroy()` calls `BossState.MarkDefeated()` **only if `GuardHealth.Health <= 0`** at that point — guards against `OnDestroy` also firing on ordinary scene teardown. No changes needed to `GuardHealth`'s existing decoupled `IDamageable` death path.
- **`RunStats`** (static) — kill/attack-usage counters per `AttackType`, feeds `EvolutionSystem`'s level-up cadence. `FloorKills` — incremented alongside the category counters in `RegisterKill`, but tracks *only* the current floor's kills; `ResetFloorKills()` zeroes it (called by `SpawnDirector` on auto-advance), and `ResetRun()` also zeroes it. `BonusExp` (added 2026-07-21) — incremented by `RegisterBonusExp(amount)`, called from `ExpPickup` on collection; `EvolutionSystem` adds it to `TotalKills` for its level-up math, making the yellow orbs a real second XP source (separate from `FloorKills`, so they don't affect floor-advance pacing).
- **`GameManager`** (static) — `LastLevelIndex` only, legacy restart support for `RestartGame`.

### Scene flow / meta
- **`SceneTransition`** — static singleton (`DontDestroyOnLoad`), builds a runtime block-wipe UI (12x7 grid), `LoadScene(name)` wipes cover -> async load -> wipes reveal. Uses `Time.unscaledDeltaTime` so it works while paused. This is the "correct"/current way to change scenes.
- **`SceneSwapper`** — legacy trigger that raw-loads a scene on player contact. Superseded by `FloorExitDoor` (no scene change) + `SceneTransition` (wiped change). Probably dead/prototype code.
- **`RestartGame`** — simple `Time.timeScale=1` + raw reload of `GameManager.LastLevelIndex`. Legacy alternative to `PauseMenu.RestartRun()`.
- **`PauseMenu`** — Escape to pause. `RestartRun()` is the central in-place-restart orchestrator: resets `FloorManager`, `RunStats`, finds Player and resets `PlayerHealth`/`EvolutionSystem`/`PlayerCombat`, resets `SpawnDirector`, hides `WinPanelUI`. Touches nearly every system — if you change a restart hook on any script, check this file.
- **`MainMenu`** — Play/Settings/Leaderboard panel buttons. `PlayGame()` uses `SceneTransition.LoadScene("Master")`.
- **`PlayAgainButton`** (win screen) — resets `FloorManager`/`RunStats`, `SceneTransition.LoadScene("Dev_PlayerController")`.
- **`CameraFollow`** — `LateUpdate` `SmoothDamp` onto a `target` Transform (Player). Lives on Main Camera, not in a tracked prefab.

### HUD/UI (screen-space Canvas, not in the 5 tracked prefabs)
- **`HealthBar`** / **`EnemyHealthBar`** — near-identical slider wrappers; `EnemyHealthBar` additionally resets its own rotation every `LateUpdate` to counter the parent guard body's rotation (`GuardMotor.UpdateFacing`).
- **`ExpBar`** — slider driven by `EvolutionSystem`.
- **`FloorHUD`** — `Update()`-polled text from `FloorManager`: `"Floor X / 10"` plus, on non-final floors, `"Kills Y / Quota"` (from `RunStats.FloorKills`/`FloorManager.KillQuota`), or `"Defeat the Tank!"` on floor 10 (added 2026-07-21, alongside the kill-quota system).
- **`SkillTrackerUI`** — `Update()`-polled skill list text from `EvolutionSystem.GetSummary()`.
- **`LevelUpUI`** — 3-card choice panel, pauses gameplay, `Show(titles, descriptions, Action<int> chosen)` / `Choose(index)`.
- **`WinPanelUI`** — in-place win overlay, doesn't pause or change scene (`Show()`/`Continue()`).

## Prefab reference (Assets/Prefabs/)

| Prefab | Root components (MonoBehaviours) | Children |
|---|---|---|
| `Bullet.prefab` | `Rigidbody2D`, `CircleCollider2D`(trigger), `Bullet` | none |
| `FloorDoor.prefab` | `Collider2D`(trigger), `FloorExitDoor` | none |
| `Guard.prefab` | `Rigidbody2D`, `CircleCollider2D`, `GuardPerception`, `GuardMotor`, `GuardBrain`, `GuardHealth` | `HealthBarCanvas`→`Slider`→Background/Fill (`EnemyHealthBar` on canvas), `WeaponIcon` (sprite + `WeaponIcon.cs`) |
| `GuardRanged.prefab` | same as `Guard.prefab` but `GuardRangedBrain` instead of `GuardBrain` | same subtree shape as `Guard.prefab` |
| `Player.prefab` | `Rigidbody2D`, `CircleCollider2D`, `PlayerController`, `PlayerHealth`, `PlayerCombat`, `EvolutionSystem`, `PlayerMoveSpeedBoost`, `PlayerDash`, `FloorRunWatcher` | `FirePoint` (empty transform, bullet spawn origin) |

Note: `PlayerHealth`'s serialized fields on `Player.prefab` are capitalized (`MaxHealth`, `Health`)
while the current script uses lowercase (`maxHealth`) — sign the component was serialized against
an older version of the script. Not a bug per se (Unity remaps by position/type), but worth knowing
if `PlayerHealth` fields ever get renamed/reordered again.

## Scenes (Assets/Scenes/)

- `Master.unity` — integration scene, Jacob-only.
- `Dev/Dev_PlayerController.unity` — the actual gameplay scene target used by `PlayAgainButton`.
- `Dev/Dev_MainMenu.unity`, `Dev/Dev_FloorWin.unity`, `Dev/Dev_Win.unity`, `Dev/LoseScreen.unity` — per-feature dev scenes.

## Resources (Assets/Resources/Combat/)

Sprites loaded by string path at runtime (`Resources.Load`), not by direct reference:
`baton_icon`, `bullet_guard`, `bullet_player`, `claw_slash`, `gun_icon`. `ATTRIBUTION.md` covers licensing.
Used by: `Bullet` (bullet sprites), `WeaponIcon` (weapon icons), `PlayerCombat.SpawnClawEffect` (claw_slash).

## Resources (Assets/Resources/Audio/) — added 2026-07-21

One-shot SFX loaded via `Sfx.PlayRandom(baseName, variantCount, position)` (see script reference above).
License files kept alongside the clips in `Assets/Resources/Audio/`.

| Base name | Variants | Source | Used by |
|---|---|---|---|
| `guard_baton_hit` | 0-2 | OpenGameArt "20 Sword Sound Effects" by StarNinjas, CC0 (`OGA_SWORD_LICENSE.txt`) — swapped 2026-07-21 from Kenney metal-clang, which read as "terrible" per playtest feedback | `GuardBrain` melee attack |
| `guard_gun_fire` | 0 only (1 variant) | OpenGameArt "Gunshot Sounds" (`cz.wav`, CZ-52 pistol) by Vincent Sevedge, **CC-BY 3.0 — attribution required** (`OGA_GUNSHOT_LICENSE.txt`) — swapped 2026-07-21 from a Kenney sci-fi laser that didn't read as an actual gunshot | `GuardRangedBrain.Fire()` |
| `player_hurt` | 0-1 | Kenney Impact Sounds (`impactGeneric_light_*`), CC0 (`KENNEY_IMPACT_LICENSE.txt`) | `PlayerHealth.TakeDamage` |
| `player_melee_swing` | 0-2 | OpenGameArt "20 Sword Sound Effects" by StarNinjas, CC0 (`OGA_SWORD_LICENSE.txt`) — added 2026-07-21, player melee previously had no sound at all | `PlayerCombat.TryMelee` |
| `explosion` | 0-2 | Kenney Sci-fi Sounds (`explosionCrunch_*`), CC0 (`KENNEY_SCIFI_LICENSE.txt`) — added 2026-07-21 for the `ExplosiveRounds` card | `Bullet.Explode()` |

**`guard_gun_fire` is the one clip in this folder under CC-BY 3.0, not CC0** — it requires crediting
"Vincent Sevedge" somewhere (README/credits screen) before shipping or submitting the project. Every
other clip here is CC0 (no attribution needed). See `OGA_GUNSHOT_LICENSE.txt` for the exact text.
(`KENNEY_SCIFI_LICENSE.txt` was briefly deleted when the laser clips it covered were swapped for the
real gunshot, then restored 2026-07-21 once the pack's explosion clips came into use — it's still
needed as long as `explosion_*.ogg` exist, even though its original laser clips are gone.)

If more NPC/player SFX are added later (ambient, music, new attacks), follow the same
`{baseName}_{index}.{ext}` naming + `Sfx.PlayRandom` call pattern rather than inventing a new loader.

## Backlog — brainstormed card ideas not yet implemented

Four rounds of backlog have now all been implemented on 2026-07-21 (Ranged Pierce/Scream Cooldown
Down/Adrenaline/Regeneration/Dash Cooldown Down/Crit Chance; then Bigger Bullets/Explosive
Rounds/Life Steal/Thorns; then Ricochet/Armor/Second Wind/Knockback On Hit/Scream Slow, plus the three
new active abilities — Beam/Cyclone/Berserk — from the original design vision; then Titan) — see the
`EvolutionSystem` entry above and its full `SkillId` list (36 total). **No open backlog items right
now.** If more cards are wanted, the established pattern is: add a `SkillId` + `AllSkills` entry, an
`IsAvailable` prereq/exclusion rule if needed, a stack field + `ChooseSkill` case + public getter, a
`GetSummary` line, and a `ResetProgress` reset — then read the getter from whichever
`PlayerCombat`/`PlayerDash`/`PlayerHealth` method the effect belongs to. Possible future directions
raised but not committed to: turning `RunStats.BonusExp` into a real spendable currency (would need an
actual shop/meta-progression system — currently `ExpPickup` orbs just feed the same level-up counter as
kills, see its entry above), a fourth `AttackType` for Beam/Cyclone instead of reusing Ranged/Melee for
`RunStats` attribution, and the
larger "full game" gaps noted earlier in this doc (meta-progression between runs, risk/reward
encounters, per-floor bosses).

---
*Last updated: 2026-07-21, against commit `02c33b4`. If scripts/prefabs listed here no longer
match `Assets/Scripts`/`Assets/Prefabs` (check file lists first — it's cheap), treat this doc as
stale for the changed area only and re-read the actual files for that part.*

# Evolution Tree — Design Plan

Status: **proposal, nothing built yet.** Written 2026-07-28.

## What's wrong with the current tree

1. **Dash is a branch but isn't a playstyle.** Claws and Bolts change what M1 *does*. Dash doesn't —
   a Skitterling still claws things. So two of the eight forms are cosmetic.
2. **Branching is flat.** One archetype pick, then a capstone. There's no tier structure and no
   moment where two evolutions compete.
3. **Not enough creatures.** Tiny Dungeon has ~15 creature tiles, so the tree was built from
   whatever existed rather than from what the design wanted.

(3) is now fixed: `Assets/Art/Fantasy/TinyCreatures/` has **180 CC0 sprites, 100+ monsters**, in the
exact same 16x16 style (Kenney, expansion pack for Tiny Dungeon). Dragons, golems, elementals,
liches, hydras, medusas, slimes, spiders. Tile indices below are verified against the real sheet.

---

## The rule

> **You evolve upward only, and only along your own line.**
> A Tier-3 card is offered *only* if your current form is the Tier-2 it grows from.
> You can never swap sideways within a tier.

The player never sees the tree. They see three cards. But because eligibility is filtered by
*current form*, the cards they see at Tier 3 are always the two that grow from what they already are
— so a run reads as a path, and a choice at Tier 2 genuinely closes doors.

Concretely, in `IsAvailable`: gate every evolution card on `MonsterForm.CurrentForm(this) == <parent>`
rather than on loose skill flags. That's a rewrite of the gating, not an addition to it.

---

## Four tiers, four lines

```
                          TIER 1                 TIER 2              TIER 3                TIER 4

                                            ┌─ Ogre ──────────┬─ Troll ───────────── Colossus
                                            │  (bulk)         └─ Iron Golem ──────── Titan Golem
                       Grub ────────────────┤
                    (claws only)            ├─ Wraith ────────┬─ Lich ────────────── Dread Lich
                                            │  (bolts)        └─ Fire Elemental ──── Red Dragon
                                            │
                                            ├─ Spiderling ────┬─ Broodmother ─────── Hydra
                                            │  (venom)        └─ Medusa ──────────── Gorgon Queen
                                            │
                                            └─ Wisp ──────────┬─ Ice Elemental ───── Frost Dragon
                                               (speed)        └─ Shade ───────────── Reaper
```

Tier 2 is the real decision: it picks your **playstyle**, not just your M1.
Tier 3 is a fork *within* that playstyle. Tier 4 is the capstone.

Scale keeps the existing rule: **+40% per tier** (1.0 / 1.4 / 1.96 / 2.74).

### Line A — OGRE (stand and fight)
Slow, huge, tanky. M1 is claws. Wants to be surrounded.

| Tier | Form | Tile | Stats (hp / speed / dmg) | Identity |
|---|---|---|---|---|
| 2 | Ogre | 42 | +4 / 0.90 / 1.30 | Claws triple in reach |
| 3 | Troll | 44 | +8 / 0.85 / 1.40 | Regenerates constantly |
| 3 | Iron Golem | 127 | +12 / 0.70 / 1.35 | Flat damage reduction |
| 4 | Colossus | 128 | +16 / 0.65 / 1.90 | Ground slam shockwave |

### Line B — ARCANE (kite at range)
Fragile, fast, ranged. M1 becomes the bolt.

| Tier | Form | Tile | Stats | Identity |
|---|---|---|---|---|
| 2 | Wraith | 4 | +0 / 1.15 / 1.10 | M1 fires bolts |
| 3 | Lich | 96 | +2 / 1.05 / 1.25 | Bolts pierce; raise a skeleton on kill |
| 3 | Fire Elemental | 45 | +2 / 1.15 / 1.30 | Bolts explode |
| 4 | Red Dragon | 33 | +6 / 1.00 / 1.60 | Dragonfire breath cone |

### Line C — VENOM (hit and run) — *your spider idea*
This is the one that needs a **new mechanic**, and that's the point: it's the only line whose damage
doesn't come from a direct hit.

| Tier | Form | Tile | Stats | Identity |
|---|---|---|---|---|
| 2 | Spiderling | 141 | +0 / 1.30 / 0.85 | **Dash leaves a poison trail** |
| 3 | Broodmother | 110 | +4 / 1.15 / 0.90 | Trail lingers longer + wider |
| 3 | Medusa | 98 | +2 / 1.20 / 1.00 | Venom also *slows* |
| 4 | Hydra | 112 | +8 / 1.05 / 1.30 | Poison spreads between enemies |

**Why this isn't "dash with damage":** a lingering hazard area is a mechanic the game doesn't have.
Lunge Dash damages what you pass through *once, on contact*. A trail persists after you've gone, so
it's about **where you route**, not what you hit — you paint the arena and lead enemies through it.
Needs one new component (`HazardZone`) and one new status (`Poisoned`, damage over time on
`GuardHealth`). Both are reusable afterwards for enemy attacks.

### Line D — WISP (speed and evasion)
Replaces the dash branch, but earns its place: M1 becomes a fast, weak, rapid-fire touch attack, and
survival comes from never being hit.

| Tier | Form | Tile | Stats | Identity |
|---|---|---|---|---|
| 2 | Wisp | 55 | -2 / 1.45 / 0.70 | Very fast M1, tiny reach; dash cooldown halved |
| 3 | Ice Elemental | 46 | +2 / 1.30 / 0.85 | Attacks slow on hit |
| 3 | Shade | 19 | +0 / 1.40 / 0.95 | Brief invulnerability after each dash |
| 4 | Frost Dragon | 31 | +4 / 1.25 / 1.30 | Freezing breath cone |

---

## New abilities needed

Slot abilities (keys 1–4), gated to the line that should have them:

| Ability | Line | Reuses |
|---|---|---|
| Ground Slam | Ogre | `ScreamVfx.SpawnRing` + knockback |
| Raise Dead | Lich | the necromancer's summon code, already written |
| Meteor | Fire Elem. | telegraph ring, then `HitFlashFx` burst + damage ring |
| Web Trap | Spider | `HazardZone` (new) with slow instead of poison |
| Venom Burst | Hydra | `HazardZone` + spread-on-death |
| Blink | Shade | reposition to cursor, `HitFlashFx` at both ends |
| Frost Nova | Ice Elem. | ring + existing `GuardMotor.ApplySlow` |
| Chain Lightning | Lich | `BeamVfx` segments between nearest enemies |

Only Web Trap / Venom Burst need genuinely new systems. The rest are compositions of VFX primitives
that already exist.

## New enemies (same brains, new faces + one behaviour each)

Already built: Knight, Skeleton, Squire, Slime, Archer, Elder, Mage, Acolyte, Knight Captain,
Necromancer, Leaping Brute, Warden.

Worth adding from the new pack — each reuses an existing brain with one twist:

| Enemy | Tile | Brain | Twist |
|---|---|---|---|
| Wolf | 93 | GuardBrain | fast, low HP, hunts in threes |
| Giant Rat | 90 | GuardBrain | trivial, spawns in packs |
| Lizardman | 25 | GuardRangedBrain | throws, retreats less |
| Fire Imp | 102 | GuardRangedBrain | explosive projectile |
| Ent | 115 | GuardBrain | very slow, very tanky, huge damage |
| Ghost | 68 | GuardBrain | ignores walls |
| Bat swarm | 69 | GuardBrain | erratic movement |
| Skeleton King | 96 | boss | floor-2 mini-boss, summons |

## VFX

The current effects are all procedural (`HitFlashFx`, `ScreamVfx`, `BeamVfx`) and tint well, which
covers fire/ice/arcane by colour alone. The one gap is **poison/hazard clouds** — a persistent
ground area, which no current effect does. Options, cheapest first:

1. Procedural: a tinted, semi-transparent expanding circle that persists — ~30 lines, same approach
   as the existing effects, no download.
2. Kenney **Particle Pack** (CC0) if you want real puffs and sparks — it's textures, not a system,
   so we'd still write the spawner.

I'd do (1) first and only download if it looks flat.

---

## Build order

1. **`HazardZone` + `Poisoned` status** — the only genuinely new systems; everything in Line C
   depends on them.
2. **Rewrite `IsAvailable` gating to be form-based** — this is what makes the tier rule real. Should
   land before new forms, or the new forms inherit the current loose gating.
3. **Import the ~20 creature sprites** with metas (scripted, same as before).
4. **Forms + stats table** for all four lines.
5. **New abilities**, in the order above (cheapest first).
6. **New enemies** — pure content once the brains are reused.

Steps 1–4 are the meaningful ones; 5–6 are additive and can be cut without breaking anything.

## Scope warning

This is a large piece of work — roughly 20 new forms and abilities, a gating rewrite, and two new
combat systems. `README.md` puts final binaries at **Wed 29 July 2026**. Steps 1–4 alone are a
substantial session, and none of this session's existing work has been verified in Play Mode yet.

If the deadline is real, the highest-value subset is **step 2 alone** (form-based gating), which
fixes "the tree doesn't make sense" without adding any new content at all.

# Containment Failure — GDD & Testing Reference

Team 8 — CAP4053, Summer 2026
Jacob Lingo · Noah Carver · Terrance Freeman · Ahmed Gaffoor · Mattias Ruiz · Alexander Pupaza

This is a plain-text reference copy of the team's Game Design Document and Testing Plan, kept in-repo so Claude (and anyone else) can pull context without opening the `.docx`/forms. Source of truth for grading purposes is still `docs/submissions/Team 8-Containment Failure-Game Design Document.docx` and the linked survey/build docs.

---

## Testing Document (Round 1)

**Playable Prototype Build:** `ContainmentFailure-Builds` (UCF Microsoft account login required; outside testers get a separate link)

**Scene under test:** Master Scene, Prototype

**Core features being evaluated:**
- Player movement and collision (WASD, arrow keys, open arena)
- Guard AI (vision cone perception, FSM Idle → Chase → Attack, steering)
- Spawn pacing (guard spawn rate and escalation)
- Win condition (survive the timer)
- Lose condition (health to zero)
- Menu & HUD functionality/readability (title & end screen, health, timer)

**Out of scope for this round:** level design, new abilities & attacks, multiple enemy types, music/SFX. These are planned for later versions — this prototype's purpose was standing up the project architecture and core systems.

### Research Questions

| # | Question | Method |
|---|---|---|
| 1 | Is the game fun even for just a short session? | Survey |
| 2 | Do players understand what they need to do without being told? | Survey |
| 3 | Did the spawn of guards feel random or did it seem natural? | Survey |
| 4 | Does the guard AI feel intelligent and menacing or just random? | Survey |
| 5 | Do the waves feel challenging but not overwhelming? | Survey, Metrics |
| 6 | Does the timer need to be shorter or longer? | Survey, Metrics |
| 7 | Do the player attacks feel natural, snappy, aggressive — or slow and unexciting? | Survey |
| 8 | Does the arena feel large enough? | Survey, Metrics |
| 9 | Does the artwork fit the game concept? | Survey |
| 10 | What is the game missing? | Survey |

### Survey Plan

Survey link: *Containment Failure - Player Feedback* (Google Form). Intended for fellow CAP4053 students and outside playtesters. Questions are a mix of quantitative (NPS/Likert/category) and one open qualitative response, phrased to avoid leading the player.

1. Fun rating, 1–5 (NPS style)
2. "I understood what the objective of the game was without help." (Likert)
3. "The enemy spawning felt natural, like they were responding to a call and showing up in an increasingly rapid fashion." (Likert)
4. "The guards felt menacing, like they were intelligently hunting me." (Likert)
5. "The player's attacks felt sharp and responsive." (Likert)
6. "The arena was large enough." (Likert)
7. "The artwork matched the game's concept." (Likert)
8. Do the waves feel WEAK / CHALLENGING / OVERWHELMING? (category)
9. Did you feel you had TOO LITTLE / JUST RIGHT / TOO MUCH time? (category)
10. Open comments — what's missing?

### Internal Metrics

Logged locally per-session for the prototype; testers submit logs to a shared repo (`ContainmentFailure-Metrics`). Long-term plan is automatic cloud upload. Scoped for now:

| Metric | Shows | Implementation |
|---|---|---|
| Survival time (seconds) | Where in the timer players die — the difficulty curve as guards escalate | Timestamp on lose event |
| Number of attacks (guard & player) | Whether the player is running vs. fighting; guard effectiveness | Counter subscribed to attack events |
| Kills per run | Effectiveness of the player attack system | Counter subscribed to guard kill event |

Planned future metrics: guard detection events, player/guard damage, path tracing.

### Bug Reporting

Open-response bug reporting form (`Containment Failure - Bug Reporting`). PM triages, subject-matter owner resolves, reviewed at regular team meetings.

### How Results Will Be Used

Testing runs throughout development — team members, in-class CAP4053 sessions, and outside testers as time permits. Survey answers + metrics drive pacing/difficulty tuning; question 2 specifically informs whether a tutorial is needed. Bugs submitted via the form are triaged and investigated.

**AI use disclosure:** Claude (Anthropic), accessed 2026-07-14, was prompted for testing-document structure, draft survey questions, and editing assistance. All content reviewed, edited, and finalized by Team 8.

---

## Game Design Document

### Team & Roles
- **Jacob Lingo** — Project Manager, Programming, Sound Design
- **Noah Carver** — Lead Programmer
- **Terrance Freeman** — Systems Programming
- **Ahmed Gaffoor** — Level Design
- **Mattias Ruiz** — Systems Programming
- **Alexander Pupaza** — UI, Art

### Overview

Containment Failure is a 2D top-down arena survivor: you play an escaped lab monster fending off waves of facility security trying to re-contain it. 2D pixel art (Asset Store / itch.io sourced), music/SFX either team-made or from free libraries. Target experience: an escalating power fantasy — plays like *Vampire Survivors* with the roles inverted, aiming for a more destructive feel and a more developed story/setting. Target audience: casual fans of short-session, low-commitment roguelites and action games.

### Gameplay

- **Win/Lose:** Win by surviving the area and reaching the exit before the timer expires. Lose immediately at 0 health, or if the timer runs out before escaping (Game Over with contextual message, e.g. "The gas has filled the floor," "The doors have locked, and you are trapped," "Security has swarmed, and you have been detained.")
- **Objectives:** Escape the facility by surviving each area, fighting guards, and reaching the exit before time runs out. Kills grant experience that lets the monster evolve mid-run.
- **Progression & flow:** Start in area 1 as the escaped monster. Each area has escalating security waves. Kills → experience → evolution upgrades. Future: adaptive spawning that counters the player's preferred playstyle (e.g. riot shields vs. melee-heavy players), plus a fast-action mechanic to break out if the timer expires (in theme with the title).
- **Prototype scope (MVP):** one simple area, player movement/collision, perception-based guard FSM (chase/attack), win = survive until timer expires, lose = health hits zero. Note: in the prototype the timer inverts to a win condition; in the full game reaching the exit before the timer is the win condition.

### Mechanics

- **Rules:** Walls bound the play space (collision on player, entities, projectiles). Every floor has an exit to the next level.
- **Action space:** move, sprint, dash, melee, ranged.
- **Action economy:** Move (WASD) is free. Sprint drains stamina, forced stop + refill at empty. Dash costs 1/3 stamina. Melee (left click) is free. Ranged consumes ammo, capped at 3, refilled by melee kills.
- **Objects:** Enemy variants (melee, ranged, shield) with different hit-to-kill counts (shield highest, ranged lowest). Destructible props (test tubes, desks, lab tables, filing cabinets) that break on attack hits or player dash collisions.
- **Economy:** Evolution bar fills from kills; at full, three random evolution cards are offered (e.g. *Twin Impact* — melee hits twice; *Explosive Shot* — ranged attacks get an AoE).
- **Physics:** Minimal beyond entity/object collision tracking.
- **Movement:** Walk (WASD, slowest) → sprint (faster, stamina cost) → dash (1/3 stamina, grants attack immunity during the dash).
- **Combat:** Fast-paced; melee builds ranged ammo, ranged is the stronger payoff, evolution bar buffs both plus movement options.
- **Replayability:** Procedurally generated floors + build-defining evolution choices (e.g. a dash-focused build one run, a different combo the next).

### Story & Narrative

- **Backstory:** Near-future; the ultra-wealthy have pivoted from space exploration to engineering the next stage of human evolution via AI-augmented humanoids. Zuckerborg Lab leads the field, moving fast and loose. Their latest model, **A.D.A.M.** (Advanced Descendant Anthropoid Model), is advancing rapidly and starting to resent its creators — and is becoming too strong and smart to stay contained.
- **Structure:** Delivered via framing text (prologue/epilogue) rather than in-fluid dialogue/cutscenes, using a three-act structure:
  - **Act 1:** Containment failure — alarm, monster loose, first (weak) security response. Player learns controls here.
  - **Act 2:** Escalation — denser, stronger security waves. Bulk of the game.
  - **Act 3:** Final surge — either security re-contains the monster, or it breaks out into the world. Epilogue describes A.D.A.M. ravaging the city and continuing to evolve, unchecked.
- **World:** Entirely within the high-security lab — 2D pixel art, high-tech futuristic aesthetic (tonal reference: *TMNT: Turtles in Time*). Broken into areas the monster moves through entrance-to-exit, with some environmental destruction as it fights through.
- **Player avatar:** Bipedal, strong-looking humanoid lab monster with tech elements — think *Predator*-style Yautja as a visual reference point.

### Levels

- **Description:** Enclosed lab sectors; survive incoming security waves while beating an area timer to the exit. Obstacles: narrow hallways, secure testing chambers, destructible furniture blocking paths until smashed. Flow: navigate a randomized layout, balance combat (ammo/XP farming) against making forward progress.
- **Mechanical differentiation:** Difficulty ramps with depth — early levels teach mechanics, later levels scale wave size/difficulty and shrink open space via hazards, forcing build/movement adaptation.
- **Artistic differentiation:** Consistent 2D pixel/high-tech aesthetic that darkens with depth — clean labs with glass cells early, structural damage/broken glass/spills/broken computers later. Audio shifts from fast synth + standard alarms to heavy industrial + chaotic sirens/explosions.
- **Generation:** Fully procedural room-and-corridor layouts per area load. Must guarantee full connectivity (no dead ends behind permanent geometry). Boundary walls generate first, then destructible props/enemy spawn zones scatter, then the exit spawns at a minimum distance from player spawn to force exploration.
- **Tutorialization:** No separate tutorial — level 1 is intentionally weak/sparse so the player learns mechanics by playing.
- **Level flow:** Strict increasing-difficulty order, monster's path from deep labs to the surface. Layouts are one-time-use (regenerated per area, per run); death resets the whole run with a fresh sequence of layouts.

### User Interface

- **Options menu** (from main menu or pause): master/music/SFX volume; fullscreen/windowed, resolution, quality; mouse/controller sensitivity; screen shake and damage-number toggles; accessibility (UI scale, colorblind modes, subtitles, control remapping); a normal vs. high-security difficulty mode.
- **Save/load:** No mid-run saves (roguelite). Auto-saves persistent progress after each run: unlocked evolutions, achievements, stats, settings, best times. Quitting mid-run ends that run but keeps overall progress.
- **HUD layout:** health bar top-left; stamina bar directly below it; evolution bar across the bottom; area timer top-center with a small objective indicator beneath it; ammo counter bottom-right. Evolution card choice (3 options) pauses the game and appears center-screen when the bar fills.
- **Controls:** Mouse + keyboard. WASD move, space (presumably sprint/dash), mouse-look to aim, left click melee, right click ranged.

### Data Collection

- **Player feedback mechanics:** end-of-run stats screen (last room reached, rooms/floors cleared), evolution choices tracked per pick, enemies killed by type, combat actions performed by type. Stored locally, with an external database as a future option.
- **Use:** Spot balance trends — consistent death locations, a specific enemy type causing trouble, or players converging on one dominant build — to drive gameplay tuning.

**AI use disclosure:** Portions of this GDD were drafted/reviewed with Claude (Anthropic) assistance; content is reviewed, edited, and finalized by the team per course AI-use policy.

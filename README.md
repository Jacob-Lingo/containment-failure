# Containment Failure

Team 8 — CAP4053 (AI for Game Programming), UCF Summer 2026

A 2D top-down arena survivor. You play A.D.A.M., an escaped lab monster fighting off waves of facility security. Vampire Survivors with the roles inverted.

**Team:** Jacob Lingo (PM / integration), Noah Carver, Terrance Freeman, Ahmed Gaffoor, Mattias Ruiz, Alexander Pupaza

## Setup

**Unity 6000.5.2f1 exactly** — check Unity Hub before opening the project. Any other version will rewrite project files and break everyone's diffs.

New to the repo? Follow **[docs/TEAM8_TEAMMATE_SETUP.md](docs/TEAM8_TEAMMATE_SETUP.md)** start to finish before your first commit. It covers Git LFS, Smart Merge, and editor setup.

All Git commands go through **Git Bash**, not PowerShell.

## Working conventions

**Scenes**
- `Assets/Scenes/Master.unity` — the integration scene. **Only Jacob edits Master**, and only to place finished prefabs.
- Everyone else works in their own dev scene: `Assets/Scenes/Dev/Dev_<YourSystem>.unity`. One per person. Never open anyone else's.
- Nobody merges scene files. If Git reports a scene conflict, stop and ping Jacob.

**Prefabs are the deliverable.** Build your system as a prefab, test it in your dev scene, tell Jacob when it's ready — he pulls it into Master. Don't build gameplay objects directly in scenes.

**Script/prefab ownership:** one owner per script and prefab. If you need a change in someone else's file (e.g., a field on the player), ask the owner — don't edit it yourself.

**Movement:** all movement goes through `Rigidbody2D` (`linearVelocity` / `MovePosition`) — never write `transform.position`. Input is read in `Update()`, physics is applied in `FixedUpdate()`.

**Sprite sorting** (`Order in Layer` on every Sprite Renderer):
| Layer | Order |
|---|---|
| Floor | -10 |
| Walls / static environment | 0 |
| Entities (player, guards, projectiles) | 10 |

**Play mode:** Inspector changes made while in Play mode are discarded on exit. Note the value, exit Play, re-enter it.

**Project settings:** Active Input Handling is set to "Both" (legacy Input API). Don't toggle it. Don't change anything under Project Settings without checking with Jacob — those files are shared.

## Repo layout

```
Assets/
  Prefabs/        finished, integration-ready prefabs
  Scenes/
    Master.unity  integration scene (Jacob only)
    Dev/          per-person dev scenes
  Scripts/        one owner per script
docs/
  TEAM8_TEAMMATE_SETUP.md   from-zero environment setup
  submissions/              GDD and graded deliverables
  reference/                course guidance docs
```

## Course context

- Prototype + midterm presentation: **Thu 7/16/2026**
- Final binaries: **Wed 7/29/2026** · Game stream: **Thu 7/30/2026**
- AI tool use is permitted by course policy with disclosure. Team members using AI tools on project work are responsible for tracking their own usage for citation.

---
*This document was drafted with Claude (Anthropic). Accessed 2026-07-12. Prompt: "Write a README for our Unity game repo covering setup, team conventions, and layout." Reviewed and edited by Jacob Lingo.*

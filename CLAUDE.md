# Containment Failure — Claude Working Notes

Unity 6000.5.2f1 project. Team conventions are in `README.md` — read that for ownership rules,
sprite sorting layers, and scene conventions. This file is about working efficiently in this
specific repo.

## Read this first, not the asset tree

**`docs/GAME_KNOWLEDGE_GRAPH.md`** is a maintained reference of every script, prefab, scene, and
system, plus the game design vision and an "implemented vs planned" table. For "what does X do",
"what depends on Y", "where does Z live", or "is feature W built yet" — read that file first.
Only fall back to grepping/reading `Assets/Scripts` or `Assets/Prefabs` directly when:
- the KG doc doesn't cover the question,
- you're about to edit a script (read the real file before editing, always — the KG is a summary, not a source of truth for exact code),
- the KG's "last updated" commit is stale relative to current `git log`.

**When you add/rename/delete a script, prefab, or major system, update the KG doc in the same
change.** A stale KG that silently misleads is worse than no KG — treat updating it as part of
the task, not a follow-up.

## Token-efficiency rules for this repo

- **Never open `.meta` files, `Library/`, `Temp/`, `Logs/`, or `UserSettings/`** — they're
  generated/binary-adjacent noise, not source of truth. `.gitignore` already excludes most of
  this from version control; don't read it back in via Glob/Grep either.
- **Prefabs and scenes are large YAML.** Don't `Read` a whole `.unity` scene file to find one
  object — `Grep` for the GameObject name or component type first, then read only the surrounding
  lines. The KG doc already has prefab component lists for the 5 tracked prefabs; check there before
  grepping.
- **Don't re-derive the dependency graph by reading every script when the KG doc already states it.**
  Cross-script relationships (who calls `FloorManager.ResetRun()`, who implements `IDamageable`,
  etc.) are listed there.
- **Scope reads to one script at a time when editing.** These files are short (most under 150
  lines); there's rarely a reason to bulk-read the whole `Scripts/` folder in one task.
- **Prefer `Grep` for "does X exist / where is X used" over spawning an Explore agent** for
  single/double lookups — only delegate to an agent for genuinely open-ended, multi-file
  investigation the KG doc doesn't already resolve.

## Unity-specific conventions (repo-wide, enforced by teammates)

- Movement goes through `Rigidbody2D` (`linearVelocity` / `MovePosition`) — never
  `transform.position` directly. Input read in `Update()`, physics applied in `FixedUpdate()`.
- `[DefaultExecutionOrder(100)]` is used on scripts (`PlayerDash`, `PlayerMoveSpeedBoost`) that
  need to run their `FixedUpdate` after `PlayerController`'s default-order one, so they can
  override velocity that tick. Follow this pattern rather than reordering scripts in the Inspector
  if you add another velocity-modifying component.
- One script/prefab owner per README — don't edit someone else's system without checking; if a
  change is needed in an owned file, say so explicitly rather than silently editing it.
- Static classes (`FloorManager`, `RunStats`, `GameManager`) hold cross-scene run state without
  `DontDestroyOnLoad` wiring — they're plain static fields, reset explicitly by
  `FloorRunWatcher`/`PauseMenu`/`PlayAgainButton`. If you add new run-scoped state, give it an
  explicit `ResetRun()`-style method and wire it into those three reset call sites, don't invent
  a fourth reset path.
- Two parallel scene-change / win / restart systems currently coexist (floor-based vs legacy
  60s-timer / build-index reload) — see "Two parallel systems" in the KG doc. Don't assume one is
  dead code without checking; flag ambiguity instead of guessing which path to extend.
- `Assets/Scenes/Master.unity` is integration-only (Jacob). Don't edit it directly.

## Before claiming a gameplay change works

This is a Unity project — type-checking C# compiles but doesn't verify feature behavior. If a
change affects live gameplay (movement, combat, spawning, UI), say explicitly that it needs
in-Editor Play Mode testing, since Claude can't run the Unity Editor. Note what to check when
the user does test it, rather than asserting it "works."

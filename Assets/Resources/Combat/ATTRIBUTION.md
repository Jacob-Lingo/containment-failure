# Combat sprite sources

All CC0 (public domain) — no attribution legally required, credited here anyway.

- `bullet_player.png`, `bullet_guard.png` — Kenney, "Top-Down Tanks" pack (opengameart.org/content/topdown-tanks)
- `gun_icon.png` — Kenney, "Topdown Shooter" pack (opengameart.org/content/topdown-shooter)
- `baton_icon.png` — "CC0 Club Icons" compilation, morning_star.png (opengameart.org/content/cc0-club-icons)
- `claw_slash.png` — "Weapon Slash - Effect", Classic set (opengameart.org/content/weapon-slash-effect)

These are placeholders — swap for the team's own art whenever ready. Loaded at
runtime via `Resources.Load<Sprite>("Combat/<name>")` from Bullet.cs,
PlayerCombat.cs, and WeaponIcon.cs, so replacing a file in place (same name)
is all that's needed to reskin.

# Team 8 — Teammate Setup Guide
### Project: `containment-failure` (CAP4053)
*Verified against Unity 6000.5.2f1 / Git for Windows / Rider, July 2026.*

Follow these steps **in order**. Everything here is one-time.

**The project and repo already exist. Do NOT create a new Unity project — you will clone the existing one.**

Two rules that prevent almost every setup problem:
1. **Run every `git` command in Git Bash, not PowerShell.** PowerShell silently mangles some of these commands.
2. **Install Unity exactly `6000.5.2f1`** — not "latest," not a newer patch. The whole team must be on the identical build or Unity scenes break when merged.

---

## 1. Accounts
- Sign in to (or create) a **GitHub account**. Send Jacob your username.
- **Accept the collaborator invite** to `Jacob-Lingo/containment-failure` — check your email or https://github.com/notifications. You cannot clone a private repo until you accept.
- You'll create a **Unity ID** with your **UCF email** in Step 4 (this grants the free student license).

## 2. Install Git for Windows
1. Download and run: https://git-scm.com/download/win — accept the defaults.
   This installs **Git Bash** (the terminal you'll use for all git commands) and Git Credential Manager (handles GitHub login).
2. Open **Git Bash** (Start menu → "Git Bash") and confirm it works:
   ```bash
   git --version
   ```

## 3. Install Git LFS
1. Download and run: https://git-lfs.com
2. In Git Bash:
   ```bash
   git lfs install
   ```
   You should see `Git LFS initialized.`

## 4. Install Unity (exact version)
1. Install **Unity Hub**: https://unity.com/download
2. Open Hub → sign in / create a Unity ID using your **UCF email** (student license).
3. Go to **Installs → Install Editor**. If `6000.5.2f1` isn't in the list, use the "other version / archive" option and select exactly **`6000.5.2f1`**.
4. Default modules are fine — Windows build support is included. You do not need Android/iOS/WebGL.
5. Confirm it reads `6000.5.2f1` under **Installs** before continuing.

## 5. Install JetBrains Rider
1. Claim the free student license: https://www.jetbrains.com/community/education (use your UCF email; approval is usually quick).
2. Install Rider and sign in with that JetBrains account when prompted.

## 6. Set your Git identity (Git Bash)
```bash
git config --global user.name "Your Name"
git config --global user.email "your_email@ucf.edu"
```

## 7. Register Unity Smart Merge — **Git Bash, NOT PowerShell**
This lets Git safely merge Unity scene/prefab files instead of corrupting them. **This is the step that breaks in PowerShell.**

1. Confirm the merge tool exists (adjust the path only if you installed Unity somewhere non-default):
   ```bash
   ls "/c/Program Files/Unity/Hub/Editor/6000.5.2f1/Editor/Data/Tools/UnityYAMLMerge.exe"
   ```
   If it prints the path, continue. If "No such file," find your install in Unity Hub (**Installs → gear icon → Show in Explorer**) and correct the path below.
2. Register the driver:
   ```bash
   git config --global merge.unityyamlmerge.name "Unity SmartMerge"
   git config --global merge.unityyamlmerge.driver '"C:/Program Files/Unity/Hub/Editor/6000.5.2f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
   git config --global merge.unityyamlmerge.recursive binary
   ```
3. Verify — the output must be the **full line ending in `%A %A`**, not a truncated fragment:
   ```bash
   git config --global --get merge.unityyamlmerge.driver
   ```
   (If it prints only `C:/Program`, you ran it in PowerShell. Redo this step in Git Bash.)

## 8. Clone the project (Git Bash)
Go to where you want the project to live, then clone:
```bash
cd ~/Desktop
git clone https://github.com/Jacob-Lingo/containment-failure.git
cd containment-failure
git lfs pull
```
- The first `git clone` opens a **browser** to log in to GitHub. **It will NOT return you to the terminal automatically — alt-tab back to Git Bash** and check for a new prompt after you finish logging in.
- `.gitignore` and `.gitattributes` are already in the repo. You never create or edit them.

## 9. Open the project in Unity
1. Unity Hub → **Add → Add project from disk** → select the cloned `containment-failure` folder.
2. Open it. It must open in **`6000.5.2f1` with no "upgrade project" prompt.** If Unity offers to upgrade, you're on the wrong version — cancel and install `6000.5.2f1`.
3. First open is slow — Unity is rebuilding its local `Library` cache. This is normal and is not a download.

## 10. Connect Rider to Unity
1. In Unity: **Edit → Preferences → External Tools → External Script Editor → Rider**.
2. Open a script: double-click any `.cs` file in the Project window, or **Assets → Open C# Project**. Rider launches.
3. Confirm the bridge works: open a MonoBehaviour script and check that Rider shows **"Event function"** tags above `Start()` and `Update()`, and that `using UnityEngine;` has no red error. If both are true, the bridge is live.

## 11. Smoke test — prove you can sync (Git Bash)
```bash
git pull
# In Unity: create an empty GameObject named "SetupTest-<yourname>", then save the scene (Ctrl+S).
git add .
git commit -m "Setup smoke test - <your name>"
git pull --no-edit
git push
```
If the push succeeds and your commit shows up on GitHub, you are fully set up.

---

## Daily workflow (once real work starts)
- **Pull when you start a session, push when you pause.** Unpushed work is invisible to the team and does not count toward your contribution record (which feeds the peer-review grade).
- Commit in logical chunks with clear messages.
- After setup, you may use Rider's or GitHub Desktop's built-in Git panel for routine pull/commit/push — only Step 7 specifically required Git Bash.
- **Don't edit the same scene as someone else at the same time.** Build your systems as prefabs and scripts and coordinate scene changes. Jacob will assign per-person ownership.

## Troubleshooting
| Problem | Cause / Fix |
|---|---|
| `git clone` → "repository not found" | You haven't accepted the collaborator invite, or aren't logged in. Accept the invite, then retry. |
| Unity prompts to "upgrade" the project | Wrong editor version. Install exactly `6000.5.2f1`. |
| Cloned art/audio look like tiny text files | LFS not active. Run `git lfs install`, then `git lfs pull`. |
| Scene merges corrupt, or Step 7 verify shows a partial path | You ran Step 7 in PowerShell. Redo it in Git Bash. |
| Browser login never returns to the terminal | Expected — alt-tab back to Git Bash; it won't auto-focus. |
| Rider shows no "Event function" tags / red `UnityEngine` errors | External editor not set. Redo Step 10.1, then reopen via **Assets → Open C# Project**. |

If anything looks off, send Jacob your Step 7 verify output and your Step 11 push result.

---
*This document was drafted with Claude (Anthropic). Accessed 2026-07-02. Prompt: "Write a from-zero onboarding runbook for teammates joining our Unity/Git project." Reviewed and edited by Jacob Lingo.*

using UnityEngine;

/// Marks a button as one MenuButtons created, so the template search can skip
/// its own output. This used to be done by name ("skip anything ending in
/// Button"), which silently excluded every real menu button in Dev_MainMenu —
/// they're all called PlayButton / SettingsButton / LeaderboardButton — so no
/// template was ever found and every clone fell back to a floating canvas
/// instead of joining the menu.
public class MenuButtonClone : MonoBehaviour
{
}

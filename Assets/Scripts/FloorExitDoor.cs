using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// The exit door — the player's escape route on every floor, per the GDD's
/// fight-to-grow vs. escape-in-time loop. On floors 1..N-1 it unlocks once
/// the floor's kill quota is met (FloorManager.KillQuota) and touching it
/// advances the floor (which also refills the floor timer). On the final
/// floor it unlocks only when the Tank is dead (BossState.Defeated) and
/// loads the escape/win scene via SceneTransition. Locked = silent no-op.
public class FloorExitDoor : MonoBehaviour
{
    [SerializeField] private string escapeSceneName = "Dev_FloorWin";
    [SerializeField] private AudioClip openSound;

    private float nextUseTime;
    private Tilemap _wallTilemap;
    private List<Vector3Int> _gatePositions;
    private bool _isGateOpen;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void Initialize(Tilemap wallTilemap, List<Vector3Int> gatePositions)
    {
        _wallTilemap = wallTilemap;
        _gatePositions = gatePositions;
    }

    private void Update()
    {
        if (_isGateOpen) return;

        bool shouldOpen = FloorManager.IsFinalFloor ? BossState.Defeated : RunStats.FloorKills >= FloorManager.KillQuota;

        if (shouldOpen)
        {
            OpenGate();
        }
    }

    private void OpenGate()
    {
        _isGateOpen = true;
        if (_wallTilemap != null && _gatePositions != null)
        {
            foreach (var pos in _gatePositions)
            {
                _wallTilemap.SetTile(pos, null);
            }
        }

        if (openSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(openSound);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || Time.time < nextUseTime) return;
        if (!_isGateOpen) return;

        nextUseTime = Time.time + 1f;

        if (FloorManager.IsFinalFloor)
        {
            // Beating the Warden isn't enough — the vault gate needs the Iron
            // Key, bought in the shop with gold banked across runs. This is the
            // deliberate "you must come back better equipped" gate.
            if (BossState.Defeated && MetaProgression.HasIronKey)
                SceneTransition.LoadScene(escapeSceneName);
            return;
        }

        // Quota met -> this door is open; walking into it is the escape.
        if (RunStats.FloorKills >= FloorManager.KillQuota)
        {
            // Advance behind the wipe: the rescale and the next floor's
            // spawns land while the screen is covered, so the change reads as
            // "descended a floor" rather than as enemies popping in.
            SceneTransition.Interstitial($"DEPTH {FloorManager.CurrentFloor + 1}", () =>
            {
                FloorManager.AdvanceFloor();
                RunStats.ResetFloorKills();

                var spawner = FindFirstObjectByType<SpawnDirector>();
                if (spawner != null) spawner.RescaleForFloor();
            });
        }
    }
}

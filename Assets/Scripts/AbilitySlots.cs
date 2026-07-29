using System;
using UnityEngine;

/// The player's four castable ability slots, bound to keys 1-4.
///
/// M1 (the archetype attack) and Dash (Shift) are deliberately NOT
/// slottable: M1 is what the level-1 card permanently decides, and dash is the
/// only escape tool against the swarm — losing either to a loadout swap makes a
/// run unplayable rather than different.
///
/// Instance state on EvolutionSystem rather than a static, so it's covered by
/// the existing ResetProgress path and needs no new reset wiring.
[Serializable]
public class AbilitySlots
{
    public const int Count = 4;

    /// Null means the slot is empty. Entries are the SkillId of an *unlock*
    /// card (UnlockScream, UnlockBeam, ...), which is what PlayerCombat
    /// dispatches on.
    private readonly SkillId?[] slots = new SkillId?[Count];

    /// The skills that occupy a slot when taken. Anything not listed here is a
    /// passive or an upgrade and never competes for space.
    public static bool IsSlottable(SkillId id) =>
        id == SkillId.UnlockScream ||
        id == SkillId.UnlockBeam ||
        id == SkillId.UnlockCyclone ||
        id == SkillId.UnlockBerserk ||
        id == SkillId.GroundSlam ||
        id == SkillId.Meteor ||
        id == SkillId.WebTrap ||
        id == SkillId.FrostNova ||
        id == SkillId.Blink ||
        id == SkillId.LeapSmash ||
        id == SkillId.ChainLightning ||
        id == SkillId.VenomBurst ||
        id == SkillId.Warp ||
        id == SkillId.Bulwark ||
        id == SkillId.Terrify ||
        id == SkillId.Maelstrom ||
        id == SkillId.FlameWheel ||
        id == SkillId.Starfall ||
        id == SkillId.BroodCall ||
        id == SkillId.Overcharge ||
        id == SkillId.Ward ||
        id == SkillId.Empower;

    public SkillId? At(int index) =>
        index >= 0 && index < Count ? slots[index] : null;

    public bool IsFull
    {
        get
        {
            for (int i = 0; i < Count; i++)
                if (slots[i] == null) return false;
            return true;
        }
    }

    /// First free slot, or -1 when full. Used to tell the player which key a
    /// card will land on *before* they pick it.
    public int FirstEmpty()
    {
        for (int i = 0; i < Count; i++)
            if (slots[i] == null) return i;
        return -1;
    }

    public int IndexOf(SkillId id)
    {
        for (int i = 0; i < Count; i++)
            if (slots[i] == id) return i;
        return -1;
    }

    /// Binds to the first empty slot. Returns false when full — the caller is
    /// expected to ask the player which slot to overwrite instead.
    public bool TryBindToEmpty(SkillId id)
    {
        if (IndexOf(id) >= 0) return true; // already bound; re-picking is a no-op

        for (int i = 0; i < Count; i++)
        {
            if (slots[i] != null) continue;
            slots[i] = id;
            return true;
        }

        return false;
    }

    public void BindTo(int index, SkillId id)
    {
        if (index < 0 || index >= Count) return;
        slots[index] = id;
    }

    public void Clear()
    {
        for (int i = 0; i < Count; i++) slots[i] = null;
    }

    /// Display name for a bound skill, or null for an empty slot. Reads the
    /// card table so renames can't desync the HUD.
    public string NameAt(int index)
    {
        SkillId? id = At(index);
        return id.HasValue ? EvolutionSystem.TitleOf(id.Value) : null;
    }

    /// KeyCode.Alpha1..Alpha4 for slot 0..3.
    public static KeyCode KeyFor(int index) => KeyCode.Alpha1 + index;

    public static string KeyLabel(int index) => (index + 1).ToString();
}

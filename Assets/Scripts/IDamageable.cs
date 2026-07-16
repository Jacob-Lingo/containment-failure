/// Shared damage contract. Anything that can take damage implements this;
/// anything that deals damage calls it via TryGetComponent. Keeps attacker
/// and defender systems decoupled — GuardBrain has no dependency on
/// PlayerHealth's type, and future damageable objects (destructible props,
/// guards themselves once the player attack lands) implement the same seam.
public interface IDamageable
{
    void TakeDamage(int amount);
}

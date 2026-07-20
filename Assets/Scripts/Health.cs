using System;
using UnityEngine;

// GDD section 14 — shared health. Death side effects (ragdoll, mission updates)
// are handled by listeners so this class stays reusable.
public class Health : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;

    public float Current { get; private set; }
    public bool IsDead { get; private set; }

    public event Action onDeath;
    public event Action onDamaged;

    // Optional hook to modify incoming damage (used for Medieval blocking).
    public Func<float, float> damageModifier;

    void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        if (damageModifier != null) damage = damageModifier(damage);
        Current = Mathf.Max(0f, Current - damage);
        if (onDamaged != null) onDamaged();
        if (Current <= 0f) Die();
    }

    void Die()
    {
        IsDead = true;
        if (onDeath != null) onDeath();
    }
}

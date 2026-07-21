using System.Collections.Generic;
using UnityEngine;

// GDD section 11 — melee swing: overlap sphere limited to a frontal arc.
public class MeleeWeapon : WeaponBase
{
    public Transform origin;       // swing center + facing (player camera)
    public float attackRadius = 2.4f;
    public float arcDegrees = 110f;

    public override void UseWeapon()
    {
        if (!CanUse()) return;
        lastUseTime = Time.time;

        // Dedupe: an enemy can expose several colliders.
        var victims = new HashSet<IDamageable>();
        Collider[] targets = Physics.OverlapSphere(origin.position, attackRadius);
        foreach (Collider target in targets)
        {
            if (target.transform.root == transform.root) continue;  // never hit self
            Vector3 to = target.bounds.center - origin.position;
            if (Vector3.Angle(origin.forward, to) > arcDegrees * 0.5f) continue;
            IDamageable d = target.GetComponentInParent<IDamageable>();
            if (d != null) victims.Add(d);
        }
        foreach (IDamageable v in victims) v.TakeDamage(damage);
        AudioDirector.SFX("whoosh", origin.position);
        NotifyFired();
    }
}

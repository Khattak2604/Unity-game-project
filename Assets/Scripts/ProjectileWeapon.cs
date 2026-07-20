using UnityEngine;

// GDD section 4 — bow with real projectile arrows (arc + drop).
public class ProjectileWeapon : WeaponBase
{
    public Transform firePoint;
    public float launchSpeed = 40f;
    public float upKick = 1.5f;          // slight lob so arrows arc
    public Collider ownerCollider;       // never hit the shooter
    public Color arrowColor = new Color(0.45f, 0.3f, 0.15f);

    public override void UseWeapon()
    {
        if (!CanUse()) return;
        lastUseTime = Time.time;
        Arrow.Launch(
            firePoint.position + firePoint.forward * 0.6f,
            firePoint.forward * launchSpeed + firePoint.up * upKick,
            damage, ownerCollider, arrowColor);
        NotifyFired();
    }
}

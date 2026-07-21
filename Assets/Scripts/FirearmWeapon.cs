using UnityEngine;

// GDD section 11 — hitscan firearm with magazine + reserve ammo and timed reload.
public class FirearmWeapon : WeaponBase
{
    public Transform firePoint;
    public float range = 100f;
    public int magazineSize = 8;
    public int reserveAmmo = 40;
    public float reloadDuration = 1.6f;
    public float spread = 0.012f;
    public bool autoFire;               // true = hold to fire (Modern/Future)
    public Color tracerColor = new Color(1f, 0.85f, 0.4f);

    public bool IsReloading { get; private set; }
    float reloadEndTime;

    void Update()
    {
        if (IsReloading && Time.time >= reloadEndTime)
        {
            IsReloading = false;
            int taken = Mathf.Min(magazineSize - ammunition, reserveAmmo);
            reserveAmmo -= taken;
            ammunition += taken;
        }
    }

    public void StartReload()
    {
        if (IsReloading || reserveAmmo <= 0 || ammunition >= magazineSize) return;
        IsReloading = true;
        reloadEndTime = Time.time + reloadDuration;
        AudioDirector.UI("reload", 0.7f);
    }

    public override void UseWeapon()
    {
        if (IsReloading || !CanUse()) return;
        if (ammunition <= 0) { StartReload(); return; }
        lastUseTime = Time.time;
        ammunition--;

        Vector3 dir = firePoint.forward
            + firePoint.right * Random.Range(-spread, spread)
            + firePoint.up * Random.Range(-spread, spread);

        Vector3 end = firePoint.position + dir * range;
        if (Physics.Raycast(firePoint.position, dir, out RaycastHit hit, range))
        {
            end = hit.point;
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(damage);
        }
        Tracer.Spawn(firePoint.position - firePoint.up * 0.12f, end, tracerColor);
        AudioDirector.SFX(sfxKey, firePoint.position);
        NotifyFired();
    }
}

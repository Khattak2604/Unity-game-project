using UnityEngine;

// GDD section 11 — base weapon class; stats live on the component so eras can
// configure them at spawn time.
public abstract class WeaponBase : MonoBehaviour
{
    public string weaponName;
    public float damage;
    public float attackRate = 2f;  // uses per second
    public int ammunition = -1;    // -1 = not ammo-based (melee)

    [HideInInspector] public WeaponViewModel viewModel;
    [HideInInspector] public string sfxKey = "";

    protected float lastUseTime = -999f;

    public bool CanUse()
    {
        return Time.time - lastUseTime >= 1f / attackRate;
    }

    // Kicks the first-person viewmodel animation (recoil / swing / bow snap).
    protected void NotifyFired()
    {
        if (viewModel != null) viewModel.PlayAttack();
    }

    public abstract void UseWeapon();
}

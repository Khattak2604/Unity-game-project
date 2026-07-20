using UnityEngine;

// GDD section 9 — era gating for shared player systems.
public class EraManager : MonoBehaviour
{
    public WarEra currentEra;

    public bool CanUseFirearms()
    {
        return currentEra != WarEra.Medieval;
    }

    public bool CanUseAdvancedMovement()
    {
        return currentEra == WarEra.Future;
    }

    public bool CanUseDrones()
    {
        return currentEra == WarEra.Modern ||
               currentEra == WarEra.Future;
    }

    public static string DisplayName(WarEra era)
    {
        switch (era)
        {
            case WarEra.Medieval: return "Medieval";
            case WarEra.WorldWarOne: return "World War I";
            case WarEra.WorldWarTwo: return "World War II";
            case WarEra.Modern: return "Modern Warfare";
            default: return "Future Warfare";
        }
    }
}

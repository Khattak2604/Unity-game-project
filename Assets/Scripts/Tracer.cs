using UnityEngine;

// ponytail: shot feedback = short-lived LineRenderer, no VFX assets; swap for
// particle/VFX-graph effects in the polish phase (GDD section 19, phase 5).
public static class Tracer
{
    public static void Spawn(Vector3 from, Vector3 to, Color color)
    {
        var go = new GameObject("Tracer");
        var line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startWidth = 0.035f;
        line.endWidth = 0.01f;
        line.material = LevelBuilder.UnlitMaterial(color);
        Object.Destroy(go, 0.06f);
    }
}

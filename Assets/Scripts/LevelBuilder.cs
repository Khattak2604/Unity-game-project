using UnityEngine;

// GDD sections 4/6/8 environments, built procedurally from primitives.
// ponytail: primitive placeholder arenas per GDD section 23 ("use placeholder
// assets until the gameplay is fun") — replace with modeled environments in
// the era-production phase.
public static class LevelBuilder
{
    public const float ArenaHalf = 22f;

    class Theme
    {
        public Color floor, wall, obstacle, fog, sun, ambient;
        public float fogDensity, sunIntensity;
        public Vector3 sunAngles;
        public bool emissiveObstacles;
    }

    static Theme For(WarEra era)
    {
        switch (era)
        {
            case WarEra.Medieval:
                return new Theme
                {
                    floor = new Color(0.35f, 0.42f, 0.24f),   // grass field
                    wall = new Color(0.45f, 0.44f, 0.42f),    // castle stone
                    obstacle = new Color(0.5f, 0.38f, 0.24f), // wooden carts/palisades
                    fog = new Color(0.75f, 0.72f, 0.6f), fogDensity = 0.006f,
                    sun = new Color(1f, 0.93f, 0.78f), sunIntensity = 1.15f,
                    sunAngles = new Vector3(45f, -35f, 0f),
                    ambient = new Color(0.45f, 0.44f, 0.38f)
                };
            case WarEra.WorldWarTwo:
                return new Theme
                {
                    floor = new Color(0.32f, 0.3f, 0.24f),    // churned earth
                    wall = new Color(0.38f, 0.36f, 0.32f),    // ruined concrete
                    obstacle = new Color(0.33f, 0.36f, 0.27f),// sandbags/crates
                    fog = new Color(0.55f, 0.55f, 0.5f), fogDensity = 0.014f,
                    sun = new Color(0.85f, 0.85f, 0.8f), sunIntensity = 0.85f,
                    sunAngles = new Vector3(38f, 160f, 0f),
                    ambient = new Color(0.35f, 0.35f, 0.33f)
                };
            default:  // Future
                return new Theme
                {
                    floor = new Color(0.12f, 0.13f, 0.17f),   // megacity deck
                    wall = new Color(0.16f, 0.18f, 0.24f),
                    obstacle = new Color(0.2f, 0.24f, 0.32f),
                    fog = new Color(0.05f, 0.07f, 0.12f), fogDensity = 0.012f,
                    sun = new Color(0.55f, 0.65f, 1f), sunIntensity = 0.7f,
                    sunAngles = new Vector3(55f, 20f, 0f),
                    ambient = new Color(0.18f, 0.2f, 0.3f),
                    emissiveObstacles = true
                };
        }
    }

    public static GameObject Build(WarEra era, Light sun, out Vector3 playerSpawn, out Vector3[] enemySpawns)
    {
        Theme t = For(era);
        var root = new GameObject("Level_" + era);

        // Lighting + atmosphere
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = t.fog;
        RenderSettings.fogDensity = t.fogDensity;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = t.ambient;
        if (sun != null)
        {
            sun.color = t.sun;
            sun.intensity = t.sunIntensity;
            sun.transform.rotation = Quaternion.Euler(t.sunAngles);
        }

        // Floor (top surface at y = 0)
        Block(root, new Vector3(0f, -0.5f, 0f), new Vector3(ArenaHalf * 2f, 1f, ArenaHalf * 2f), t.floor, false);

        // Perimeter walls
        float w = ArenaHalf * 2f + 2f;
        Block(root, new Vector3(0f, 2f, ArenaHalf + 0.5f), new Vector3(w, 4f, 1f), t.wall, false);
        Block(root, new Vector3(0f, 2f, -ArenaHalf - 0.5f), new Vector3(w, 4f, 1f), t.wall, false);
        Block(root, new Vector3(ArenaHalf + 0.5f, 2f, 0f), new Vector3(1f, 4f, w), t.wall, false);
        Block(root, new Vector3(-ArenaHalf - 0.5f, 2f, 0f), new Vector3(1f, 4f, w), t.wall, false);

        // Cover obstacles — deterministic layout per era, center lane kept clear.
        var rng = new System.Random(100 + (int)era);
        for (int i = 0; i < 14; i++)
        {
            float x = Mathf.Lerp(-ArenaHalf + 3f, ArenaHalf - 3f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-ArenaHalf + 3f, ArenaHalf - 3f, (float)rng.NextDouble());
            if (Mathf.Abs(x) < 3.5f && Mathf.Abs(z) < 3.5f) continue;  // keep spawn area open
            float sx = Mathf.Lerp(1.2f, 4f, (float)rng.NextDouble());
            float sy = Mathf.Lerp(1f, 2.4f, (float)rng.NextDouble());
            float sz = Mathf.Lerp(1.2f, 4f, (float)rng.NextDouble());
            Block(root, new Vector3(x, sy * 0.5f, z), new Vector3(sx, sy, sz), t.obstacle, t.emissiveObstacles);
        }

        playerSpawn = new Vector3(0f, 1.2f, -ArenaHalf + 6f);

        // Enemy ring across the far half of the arena.
        int count = 8;
        enemySpawns = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Lerp(-70f, 70f, i / (float)(count - 1)) * Mathf.Deg2Rad;
            float radius = 13f + 5f * (float)rng.NextDouble();
            enemySpawns[i] = new Vector3(Mathf.Sin(angle) * radius, 1.2f, Mathf.Cos(angle) * radius * 0.85f + 2f);
        }
        return root;
    }

    public static EnemyAI SpawnEnemy(WarEra era, Vector3 position, Transform target, Transform parent)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(parent);
        body.transform.position = position;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(head.GetComponent<Collider>());
        head.transform.SetParent(body.transform);
        head.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        head.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);

        var cc = body.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;

        var health = body.AddComponent<Health>();
        var ai = body.AddComponent<EnemyAI>();
        ai.SetTarget(target);

        Color bodyColor, headColor;
        switch (era)
        {
            case WarEra.Medieval:  // swordsman — fast melee rusher
                body.name = "Medieval Swordsman";
                bodyColor = new Color(0.55f, 0.15f, 0.12f); headColor = new Color(0.7f, 0.7f, 0.72f);
                ai.moveSpeed = 4.6f; ai.attackRange = 2.3f; ai.attackDamage = 14f;
                ai.attackRate = 1.1f; ai.attackWindup = 0.4f;
                health.maxHealth = 70f;
                break;
            case WarEra.WorldWarTwo:  // rifleman — keeps distance, aimed shots
                body.name = "WWII Rifleman";
                bodyColor = new Color(0.34f, 0.38f, 0.25f); headColor = new Color(0.42f, 0.4f, 0.3f);
                ai.isRanged = true; ai.moveSpeed = 3.2f; ai.attackRange = 15f;
                ai.attackDamage = 9f; ai.attackRate = 0.8f; ai.attackWindup = 0.55f;
                ai.rangedSpread = 0.07f; ai.tracerColor = new Color(1f, 0.85f, 0.45f);
                health.maxHealth = 60f;
                break;
            default:  // Future combat bot — energy fire, dashes sideways
                body.name = "Future Combat Bot";
                bodyColor = new Color(0.15f, 0.17f, 0.2f); headColor = new Color(0.2f, 0.9f, 1f);
                ai.isRanged = true; ai.canDash = true; ai.moveSpeed = 4.2f;
                ai.attackRange = 13f; ai.attackDamage = 7f; ai.attackRate = 1.6f;
                ai.attackWindup = 0.3f; ai.rangedSpread = 0.05f;
                ai.tracerColor = new Color(0.3f, 0.95f, 1f);
                health.maxHealth = 80f;
                break;
        }
        body.GetComponent<Renderer>().material = ColoredMaterial(bodyColor, false);
        head.GetComponent<Renderer>().material = ColoredMaterial(headColor, era == WarEra.Future);
        return ai;
    }

    // --- materials -----------------------------------------------------------

    static readonly System.Collections.Generic.Dictionary<Color, Material> litCache =
        new System.Collections.Generic.Dictionary<Color, Material>();
    static readonly System.Collections.Generic.Dictionary<Color, Material> unlitCache =
        new System.Collections.Generic.Dictionary<Color, Material>();

    public static Material ColoredMaterial(Color c, bool emissive)
    {
        if (!emissive && litCache.TryGetValue(c, out Material cached)) return cached;
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");  // ancient fallback, editor always has Standard
        var m = new Material(shader) { color = c };
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 1.6f);
        }
        else litCache[c] = m;
        return m;
    }

    public static Material UnlitMaterial(Color c)
    {
        if (unlitCache.TryGetValue(c, out Material cached)) return cached;
        Shader shader = Shader.Find("Unlit/Color");
        var m = new Material(shader) { color = c };
        unlitCache[c] = m;
        return m;
    }

    static void Block(GameObject root, Vector3 pos, Vector3 scale, Color color, bool emissive)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = ColoredMaterial(color, emissive);
    }
}

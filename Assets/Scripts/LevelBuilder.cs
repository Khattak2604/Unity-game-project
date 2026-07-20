using UnityEngine;

// GDD sections 4-8 environments, built procedurally from primitives.
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
        public int obstacleCount = 14;
    }

    static Theme For(WarEra era)
    {
        switch (era)
        {
            case WarEra.Medieval:
                return new Theme
                {
                    floor = new Color(0.35f, 0.42f, 0.24f),    // grass field
                    wall = new Color(0.45f, 0.44f, 0.42f),     // castle stone
                    obstacle = new Color(0.5f, 0.38f, 0.24f),  // carts / palisades
                    fog = new Color(0.75f, 0.72f, 0.6f), fogDensity = 0.006f,
                    sun = new Color(1f, 0.93f, 0.78f), sunIntensity = 1.15f,
                    sunAngles = new Vector3(45f, -35f, 0f),
                    ambient = new Color(0.45f, 0.44f, 0.38f),
                    obstacleCount = 10
                };
            case WarEra.WorldWarOne:
                return new Theme
                {
                    floor = new Color(0.3f, 0.26f, 0.2f),      // mud
                    wall = new Color(0.34f, 0.3f, 0.24f),
                    obstacle = new Color(0.36f, 0.32f, 0.24f), // timber / debris
                    fog = new Color(0.52f, 0.5f, 0.44f), fogDensity = 0.02f,
                    sun = new Color(0.75f, 0.72f, 0.65f), sunIntensity = 0.65f,
                    sunAngles = new Vector3(30f, 140f, 0f),
                    ambient = new Color(0.32f, 0.3f, 0.27f),
                    obstacleCount = 6                          // trenches carry the layout
                };
            case WarEra.WorldWarTwo:
                return new Theme
                {
                    floor = new Color(0.32f, 0.3f, 0.24f),     // churned earth
                    wall = new Color(0.38f, 0.36f, 0.32f),     // ruined concrete
                    obstacle = new Color(0.33f, 0.36f, 0.27f), // sandbags / crates
                    fog = new Color(0.55f, 0.55f, 0.5f), fogDensity = 0.014f,
                    sun = new Color(0.85f, 0.85f, 0.8f), sunIntensity = 0.85f,
                    sunAngles = new Vector3(38f, 160f, 0f),
                    ambient = new Color(0.35f, 0.35f, 0.33f),
                    obstacleCount = 12
                };
            case WarEra.Modern:
                return new Theme
                {
                    floor = new Color(0.24f, 0.24f, 0.26f),    // asphalt
                    wall = new Color(0.3f, 0.31f, 0.34f),      // concrete barrier
                    obstacle = new Color(0.38f, 0.34f, 0.26f), // crates / barricades
                    fog = new Color(0.5f, 0.53f, 0.58f), fogDensity = 0.01f,
                    sun = new Color(0.95f, 0.93f, 0.88f), sunIntensity = 0.95f,
                    sunAngles = new Vector3(52f, 25f, 0f),
                    ambient = new Color(0.36f, 0.37f, 0.4f),
                    obstacleCount = 10
                };
            default:  // Future
                return new Theme
                {
                    floor = new Color(0.12f, 0.13f, 0.17f),    // megacity deck
                    wall = new Color(0.16f, 0.18f, 0.24f),
                    obstacle = new Color(0.2f, 0.24f, 0.32f),
                    fog = new Color(0.05f, 0.07f, 0.12f), fogDensity = 0.012f,
                    sun = new Color(0.55f, 0.65f, 1f), sunIntensity = 0.7f,
                    sunAngles = new Vector3(55f, 20f, 0f),
                    ambient = new Color(0.18f, 0.2f, 0.3f),
                    emissiveObstacles = true,
                    obstacleCount = 12
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
        for (int i = 0; i < t.obstacleCount; i++)
        {
            float x = Mathf.Lerp(-ArenaHalf + 3f, ArenaHalf - 3f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-ArenaHalf + 3f, ArenaHalf - 3f, (float)rng.NextDouble());
            if (Mathf.Abs(x) < 3.5f && Mathf.Abs(z) < 3.5f) continue;  // keep spawn area open
            float sx = Mathf.Lerp(1.2f, 4f, (float)rng.NextDouble());
            float sy = Mathf.Lerp(1f, 2.4f, (float)rng.NextDouble());
            float sz = Mathf.Lerp(1.2f, 4f, (float)rng.NextDouble());
            Block(root, new Vector3(x, sy * 0.5f, z), new Vector3(sx, sy, sz), t.obstacle, t.emissiveObstacles);
        }

        BuildEraStructures(root, era, t, rng);

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

    // Era-identity landmarks + machinery (GDD sections 4-8 environments).
    static void BuildEraStructures(GameObject root, WarEra era, Theme t, System.Random rng)
    {
        Color rust = new Color(0.35f, 0.24f, 0.16f);
        Color wood = new Color(0.4f, 0.28f, 0.15f);
        switch (era)
        {
            case WarEra.Medieval:
                // corner towers + crenellated wall tops
                foreach (var sx in new[] { -1f, 1f })
                    foreach (var sz in new[] { -1f, 1f })
                        Cylinder(root, new Vector3(sx * ArenaHalf, 3f, sz * ArenaHalf), 1.6f, 6f, t.wall);
                for (float x = -ArenaHalf + 1f; x <= ArenaHalf - 1f; x += 2.5f)
                {
                    Block(root, new Vector3(x, 4.3f, ArenaHalf + 0.5f), new Vector3(1.1f, 0.6f, 1f), t.wall, false);
                    Block(root, new Vector3(x, 4.3f, -ArenaHalf - 0.5f), new Vector3(1.1f, 0.6f, 1f), t.wall, false);
                }
                // siege catapult (GDD section 4 siege interactions)
                Block(root, new Vector3(-9f, 0.5f, 2f), new Vector3(3.2f, 1f, 1.8f), wood, false);
                BlockRot(root, new Vector3(-9f, 1.9f, 1.6f), new Vector3(0.35f, 3.4f, 0.3f), new Vector3(-40f, 0f, 0f), wood);
                Block(root, new Vector3(-9f, 3.35f, 0.4f), new Vector3(0.9f, 0.35f, 0.9f), wood, false);   // basket
                Wheel(root, new Vector3(-10.4f, 0.5f, 1f), 0.55f, wood);
                Wheel(root, new Vector3(-7.6f, 0.5f, 1f), 0.55f, wood);
                Wheel(root, new Vector3(-10.4f, 0.5f, 3f), 0.55f, wood);
                Wheel(root, new Vector3(-7.6f, 0.5f, 3f), 0.55f, wood);
                break;

            case WarEra.WorldWarOne:
                // trench lines with passage gaps + shell craters
                foreach (float z in new[] { -5f, 3f, 11f })
                {
                    Block(root, new Vector3(-12.5f, 0.55f, z), new Vector3(17f, 1.1f, 0.9f), t.wall, false);
                    Block(root, new Vector3(12.5f, 0.55f, z), new Vector3(17f, 1.1f, 0.9f), t.wall, false);
                }
                for (int i = 0; i < 10; i++)
                {
                    float cx = Mathf.Lerp(-18f, 18f, (float)rng.NextDouble());
                    float cz = Mathf.Lerp(-16f, 18f, (float)rng.NextDouble());
                    Decal(root, new Vector3(cx, 0.03f, cz), new Vector3(2.8f, 0.06f, 2.8f), t.floor * 0.55f);
                }
                // stranded early tank (GDD section 5 vehicle section) — rhomboid hull + tracks
                Block(root, new Vector3(8f, 1.1f, 0f), new Vector3(2.2f, 1.6f, 4.6f), rust, false);          // hull
                BlockRot(root, new Vector3(8f, 1.7f, 2.6f), new Vector3(2.2f, 1.1f, 1.6f), new Vector3(35f, 0f, 0f), rust); // sloped nose
                Block(root, new Vector3(6.7f, 0.85f, 0f), new Vector3(0.5f, 1.7f, 5.2f), rust * 0.8f, false); // left track
                Block(root, new Vector3(9.3f, 0.85f, 0f), new Vector3(0.5f, 1.7f, 5.2f), rust * 0.8f, false); // right track
                BlockRot(root, new Vector3(6.7f, 1.6f, 1.2f), new Vector3(0.35f, 0.35f, 1.6f), new Vector3(0f, 0f, 0f), rust * 0.6f); // side gun sponson
                break;

            case WarEra.WorldWarTwo:
                // sandbag rows + ruined building shells
                foreach (float z in new[] { -2f, 8f })
                {
                    Block(root, new Vector3(-8f, 0.45f, z), new Vector3(6f, 0.9f, 0.8f), new Color(0.5f, 0.45f, 0.32f), false);
                    Block(root, new Vector3(8f, 0.45f, z), new Vector3(6f, 0.9f, 0.8f), new Color(0.5f, 0.45f, 0.32f), false);
                }
                Block(root, new Vector3(-15f, 2.5f, 14f), new Vector3(6f, 5f, 0.6f), t.wall, false);
                Block(root, new Vector3(15f, 1.8f, 12f), new Vector3(0.6f, 3.6f, 7f), t.wall, false);
                // knocked-out tank with turret (GDD section 6 tank battle)
                Block(root, new Vector3(-8f, 0.9f, 1f), new Vector3(2.6f, 1.3f, 4.4f), new Color(0.3f, 0.32f, 0.26f), false);   // hull
                Block(root, new Vector3(-8f, 2f, 0.6f), new Vector3(1.7f, 0.9f, 2f), new Color(0.28f, 0.3f, 0.24f), false);     // turret
                BlockRot(root, new Vector3(-8f, 2.15f, 2.6f), new Vector3(0.22f, 0.22f, 2.6f), new Vector3(-4f, 0f, 0f), new Color(0.22f, 0.24f, 0.2f)); // gun
                BlockRot(root, new Vector3(-6.4f, 0.5f, 3.4f), new Vector3(0.45f, 1.4f, 2f), new Vector3(0f, 30f, 75f), rust);  // thrown track
                break;

            case WarEra.Modern:
                // city blocks around the edges + container stacks
                foreach (var b in new[] {
                    new Vector4(-16f, 15f, 6f, 11f), new Vector4(16f, 15f, 7f, 9f),
                    new Vector4(-16f, -2f, 5f, 13f), new Vector4(16f, 1f, 6f, 8f) })
                {
                    Block(root, new Vector3(b.x, b.w * 0.5f, b.y), new Vector3(b.z, b.w, 5f), t.wall, false);
                }
                Block(root, new Vector3(-6f, 1.25f, 10f), new Vector3(2.5f, 2.5f, 6f), new Color(0.6f, 0.3f, 0.2f), false);
                Block(root, new Vector3(6f, 1.25f, 4f), new Vector3(6f, 2.5f, 2.5f), new Color(0.25f, 0.4f, 0.5f), false);
                // armored patrol vehicle (GDD section 7 convoy)
                Block(root, new Vector3(9f, 1.1f, 0f), new Vector3(2.2f, 1.4f, 5f), new Color(0.22f, 0.24f, 0.22f), false);     // hull
                Block(root, new Vector3(9f, 2.05f, -0.8f), new Vector3(1.8f, 0.6f, 2f), new Color(0.2f, 0.22f, 0.2f), false);   // cab
                Block(root, new Vector3(9f, 2.5f, 0.6f), new Vector3(0.5f, 0.35f, 0.9f), new Color(0.16f, 0.17f, 0.16f), false); // turret gun
                Wheel(root, new Vector3(7.8f, 0.55f, 1.7f), 0.6f, DarkTire); Wheel(root, new Vector3(10.2f, 0.55f, 1.7f), 0.6f, DarkTire);
                Wheel(root, new Vector3(7.8f, 0.55f, -1.7f), 0.6f, DarkTire); Wheel(root, new Vector3(10.2f, 0.55f, -1.7f), 0.6f, DarkTire);
                break;

            case WarEra.Future:
                // neon pylons
                foreach (var p in new[] { new Vector3(-14f, 0f, 8f), new Vector3(14f, 0f, 8f),
                                          new Vector3(-8f, 0f, 16f), new Vector3(8f, 0f, 16f) })
                    Block(root, new Vector3(p.x, 3.5f, p.z), new Vector3(0.5f, 7f, 0.5f), new Color(0.25f, 0.9f, 1f), true);
                // downed combat mech (GDD section 8) — torso, cockpit, legs, arm cannon
                Color mech = new Color(0.18f, 0.2f, 0.24f);
                Block(root, new Vector3(-9f, 1.6f, 1f), new Vector3(2.4f, 2f, 1.6f), mech, false);            // torso
                Block(root, new Vector3(-9f, 2.4f, 1.8f), new Vector3(1.2f, 0.8f, 0.6f), new Color(0.25f, 0.9f, 1f), true); // cockpit glow
                BlockRot(root, new Vector3(-10.6f, 0.9f, 0.2f), new Vector3(0.8f, 2.4f, 0.8f), new Vector3(0f, 0f, 55f), mech);  // collapsed leg
                BlockRot(root, new Vector3(-7.5f, 0.7f, 1.9f), new Vector3(0.8f, 2.2f, 0.8f), new Vector3(60f, 0f, 0f), mech);   // collapsed leg
                BlockRot(root, new Vector3(-7.4f, 1.9f, 0.2f), new Vector3(0.5f, 0.5f, 2.4f), new Vector3(0f, -25f, 0f), mech * 0.8f); // arm cannon
                break;
        }
    }

    public static EnemyAI SpawnEnemy(WarEra era, Vector3 position, Transform target, Transform parent)
    {
        // Root carries physics + logic; the humanoid look lives on a child (EnemyVisual).
        var body = new GameObject("Enemy");
        body.transform.SetParent(parent);
        body.transform.position = position;

        var cc = body.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;

        var health = body.AddComponent<Health>();
        var ai = body.AddComponent<EnemyAI>();
        ai.SetTarget(target);

        Color uniform, helmet;
        bool ranged;
        switch (era)
        {
            case WarEra.Medieval:  // swordsman — fast melee rusher
                body.name = "Medieval Swordsman";
                uniform = new Color(0.55f, 0.15f, 0.12f); helmet = new Color(0.7f, 0.7f, 0.72f);
                ranged = false;
                ai.moveSpeed = 4.6f; ai.attackRange = 2.3f; ai.attackDamage = 14f;
                ai.attackRate = 1.1f; ai.attackWindup = 0.4f;
                health.maxHealth = 70f;
                break;
            case WarEra.WorldWarOne:  // trench rifleman — slow deliberate bolt-action
                body.name = "Trench Rifleman";
                uniform = new Color(0.42f, 0.38f, 0.3f); helmet = new Color(0.35f, 0.33f, 0.28f);
                ranged = true;
                ai.isRanged = true; ai.moveSpeed = 2.8f; ai.attackRange = 16f;
                ai.attackDamage = 14f; ai.attackRate = 0.45f; ai.attackWindup = 0.75f;
                ai.rangedSpread = 0.05f; ai.tracerColor = new Color(1f, 0.8f, 0.4f);
                health.maxHealth = 65f;
                break;
            case WarEra.WorldWarTwo:  // rifleman — keeps distance, aimed shots
                body.name = "WWII Rifleman";
                uniform = new Color(0.34f, 0.38f, 0.25f); helmet = new Color(0.3f, 0.32f, 0.24f);
                ranged = true;
                ai.isRanged = true; ai.moveSpeed = 3.2f; ai.attackRange = 15f;
                ai.attackDamage = 9f; ai.attackRate = 0.8f; ai.attackWindup = 0.55f;
                ai.rangedSpread = 0.07f; ai.tracerColor = new Color(1f, 0.85f, 0.45f);
                health.maxHealth = 60f;
                break;
            case WarEra.Modern:  // urban soldier — faster fire, tougher
                body.name = "Modern Soldier";
                uniform = new Color(0.25f, 0.27f, 0.24f); helmet = new Color(0.15f, 0.16f, 0.15f);
                ranged = true;
                ai.isRanged = true; ai.moveSpeed = 3.8f; ai.attackRange = 14f;
                ai.attackDamage = 6f; ai.attackRate = 2.2f; ai.attackWindup = 0.35f;
                ai.rangedSpread = 0.07f; ai.tracerColor = new Color(1f, 0.9f, 0.6f);
                health.maxHealth = 90f;
                break;
            default:  // Future combat bot — energy fire, dashes sideways
                body.name = "Future Combat Bot";
                uniform = new Color(0.15f, 0.17f, 0.2f); helmet = new Color(0.2f, 0.9f, 1f);
                ranged = true;
                ai.isRanged = true; ai.canDash = true; ai.moveSpeed = 4.2f;
                ai.attackRange = 13f; ai.attackDamage = 7f; ai.attackRate = 1.6f;
                ai.attackWindup = 0.3f; ai.rangedSpread = 0.05f;
                ai.tracerColor = new Color(0.3f, 0.95f, 1f);
                health.maxHealth = 80f;
                break;
        }
        ai.visual = EnemyVisual.Build(body.transform, era, uniform, helmet, ranged);
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

    static readonly Color DarkTire = new Color(0.1f, 0.1f, 0.11f);

    static void Block(GameObject root, Vector3 pos, Vector3 scale, Color color, bool emissive)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = ColoredMaterial(color, emissive);
    }

    static void BlockRot(GameObject root, Vector3 pos, Vector3 scale, Vector3 euler, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = ColoredMaterial(color, false);
    }

    // wheel: cylinder lying on its side, axle along x
    static void Wheel(GameObject root, Vector3 pos, float radius, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        go.transform.localScale = new Vector3(radius * 2f, 0.15f, radius * 2f);
        go.GetComponent<Renderer>().material = ColoredMaterial(color, false);
    }

    static void Cylinder(GameObject root, Vector3 pos, float radius, float height, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        go.GetComponent<Renderer>().material = ColoredMaterial(color, false);
    }

    // flat visual-only marking (no collider) — shell craters, scorch marks
    static void Decal(GameObject root, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = ColoredMaterial(color, false);
    }
}

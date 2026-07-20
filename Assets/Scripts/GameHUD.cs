using UnityEngine;

// GDD section 15 — main menu, gameplay HUD and overlays.
// ponytail: immediate-mode GUI so the repo needs zero UI assets/prefabs;
// port to uGUI/TextMeshPro with era-themed skins in the polish phase.
public class GameHUD : MonoBehaviour
{
    // Full campaign list for the menu (GDD section 22); MVP maps three of them.
    static readonly WarEra[] MenuOrder =
    {
        WarEra.Medieval, WarEra.WorldWarOne, WarEra.WorldWarTwo, WarEra.Modern, WarEra.Future
    };

    GUIStyle title, subtitle, button, label, big, small;
    float damageFlash;
    Health hookedHealth;

    void OnGUI()
    {
        EnsureStyles();
        var gm = GameManager.Instance;
        if (gm == null) return;

        switch (gm.State)
        {
            case GameManager.GameState.Menu: DrawMenu(gm); break;
            case GameManager.GameState.Playing: DrawHud(gm); break;
            case GameManager.GameState.Won: DrawHud(gm); DrawWon(gm); break;
            case GameManager.GameState.Dead: DrawHud(gm); DrawDead(gm); break;
            case GameManager.GameState.Paused: DrawHud(gm); DrawPaused(gm); break;
        }
    }

    float S { get { return Screen.height / 1080f; } }

    void EnsureStyles()
    {
        if (title != null) return;
        title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        subtitle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        button = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };
        label = new GUIStyle(GUI.skin.label);
        big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
    }

    void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    Rect Centered(float w, float h, float y)
    {
        return new Rect((Screen.width - w) * 0.5f, y, w, h);
    }

    // --- menu ----------------------------------------------------------------

    void DrawMenu(GameManager gm)
    {
        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.05f, 0.06f, 0.08f, 0.92f));

        title.fontSize = (int)(78 * S);
        title.normal.textColor = new Color(0.92f, 0.88f, 0.78f);
        GUI.Label(Centered(1000 * S, 100 * S, 90 * S), "EVOLUTION OF WAR", title);

        subtitle.fontSize = (int)(26 * S);
        subtitle.normal.textColor = new Color(0.6f, 0.6f, 0.62f);
        GUI.Label(Centered(1000 * S, 40 * S, 190 * S),
            "One bloodline. Five centuries of warfare.", subtitle);

        button.fontSize = (int)(28 * S);
        float y = 300 * S, w = 620 * S, h = 64 * S, step = 78 * S;

        foreach (WarEra era in MenuOrder)
        {
            int chapterIndex = System.Array.IndexOf(GameManager.Chapters, era);
            string name = EraManager.DisplayName(era);
            Rect r = Centered(w, h, y);
            y += step;

            bool unlocked = gm.IsChapterUnlocked(chapterIndex);
            GUI.enabled = unlocked;
            string text = unlocked ? name : name + "  —  LOCKED (finish previous chapter)";
            if (GUI.Button(r, text, button)) gm.StartChapter(chapterIndex);
            GUI.enabled = true;
        }

        if (GUI.Button(Centered(240 * S, 52 * S, y + 16 * S), "Quit", button)) gm.QuitGame();

        small.fontSize = (int)(19 * S);
        small.normal.textColor = new Color(0.5f, 0.5f, 0.52f);
        GUI.Label(Centered(1500 * S, 60 * S, Screen.height - 70 * S),
            "WASD move · Mouse look · LMB attack · RMB block (melee) · 1-2/scroll switch weapon · R reload · Space jump (double in Future) · Q dash (Future) · Shift sprint · Esc pause",
            small);
    }

    // --- gameplay HUD --------------------------------------------------------

    void DrawHud(GameManager gm)
    {
        if (gm.PlayerHealth == null) return;
        HookDamageFlash(gm);

        // damage vignette
        if (damageFlash > 0f)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.7f, 0f, 0f, damageFlash * 0.35f));
            if (gm.State == GameManager.GameState.Playing) damageFlash -= Time.deltaTime * 2.2f;
        }

        // crosshair
        Fill(new Rect(Screen.width / 2f - 3f, Screen.height / 2f - 3f, 6f, 6f), new Color(1f, 1f, 1f, 0.85f));

        // health bar (bottom left)
        float w = 360 * S, h = 26 * S, x = 30 * S, yb = Screen.height - 64 * S;
        Fill(new Rect(x - 3, yb - 3, w + 6, h + 6), new Color(0f, 0f, 0f, 0.55f));
        float frac = Mathf.Clamp01(gm.PlayerHealth.Current / gm.PlayerHealth.maxHealth);
        Fill(new Rect(x, yb, w * frac, h), Color.Lerp(new Color(0.75f, 0.15f, 0.1f), new Color(0.2f, 0.75f, 0.3f), frac));
        label.fontSize = (int)(20 * S);
        label.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 8 * S, yb - 1, w, h), "HP  " + Mathf.CeilToInt(gm.PlayerHealth.Current), label);

        // dash cooldown (Future)
        if (gm.Player != null && gm.Player.advancedMovement)
        {
            float ready = 1f - Mathf.Clamp01(gm.Player.DashReadyIn / PlayerController.DashCooldown);
            Fill(new Rect(x - 3, yb - 18 * S, w * 0.5f + 6, 10 * S), new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(x, yb - 16 * S, w * 0.5f * ready, 7 * S),
                ready >= 1f ? new Color(0.3f, 0.95f, 1f) : new Color(0.25f, 0.45f, 0.55f));
        }

        // weapon / ammo (bottom right)
        WeaponBase current = gm.Player != null ? gm.Player.CurrentWeapon : null;
        var firearm = current as FirearmWeapon;
        label.fontSize = (int)(26 * S);
        string wtext;
        if (firearm != null)
            wtext = firearm.IsReloading
                ? firearm.weaponName + "  RELOADING…"
                : firearm.weaponName + "   " + firearm.ammunition + " / " + firearm.reserveAmmo;
        else if (current is MeleeWeapon)
            wtext = current.weaponName + (gm.Player.IsBlocking ? "   [BLOCKING]" : "   (RMB to block)");
        else if (current != null)
            wtext = current.weaponName;
        else wtext = "";
        var wRect = new Rect(Screen.width - 560 * S, Screen.height - 64 * S, 530 * S, 30 * S);
        Fill(new Rect(wRect.x - 6, wRect.y - 3, wRect.width + 12, wRect.height + 6), new Color(0f, 0f, 0f, 0.45f));
        label.alignment = TextAnchor.MiddleRight;
        GUI.Label(wRect, wtext, label);

        // weapon slots (switch with 1-2 / scroll)
        if (gm.Player != null && gm.Player.weapons.Count > 1)
        {
            small.fontSize = (int)(17 * S);
            small.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
            string slots = "";
            for (int i = 0; i < gm.Player.weapons.Count; i++)
            {
                bool act = i == gm.Player.ActiveWeaponIndex;
                slots += (act ? "[" : " ") + (i + 1) + " " + gm.Player.weapons[i].weaponName + (act ? "]" : " ") + "   ";
            }
            small.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(Screen.width - 560 * S, Screen.height - 32 * S, 530 * S, 22 * S),
                slots + "· scroll/1-2", small);
            small.alignment = TextAnchor.MiddleCenter;
        }
        label.alignment = TextAnchor.UpperLeft;

        // objective (top left) + era (top right)
        if (gm.Objective != null)
        {
            label.fontSize = (int)(22 * S);
            var oRect = new Rect(24 * S, 20 * S, 640 * S, 34 * S);
            Fill(new Rect(oRect.x - 6, oRect.y - 3, oRect.width + 12, oRect.height + 6), new Color(0f, 0f, 0f, 0.45f));
            GUI.Label(oRect, "OBJECTIVE — " + gm.Objective.ProgressText(), label);
        }
        label.fontSize = (int)(22 * S);
        label.alignment = TextAnchor.MiddleRight;
        var eRect = new Rect(Screen.width - 424 * S, 20 * S, 400 * S, 34 * S);
        Fill(new Rect(eRect.x - 6, eRect.y - 3, eRect.width + 12, eRect.height + 6), new Color(0f, 0f, 0f, 0.45f));
        GUI.Label(eRect, EraManager.DisplayName(gm.eraManager.currentEra), label);
        label.alignment = TextAnchor.UpperLeft;
    }

    void HookDamageFlash(GameManager gm)
    {
        if (hookedHealth == gm.PlayerHealth) return;
        hookedHealth = gm.PlayerHealth;
        hookedHealth.onDamaged += () => damageFlash = 1f;
    }

    // --- overlays ------------------------------------------------------------

    void Overlay(string heading, Color headingColor)
    {
        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.65f));
        big.fontSize = (int)(64 * S);
        big.normal.textColor = headingColor;
        GUI.Label(Centered(1000 * S, 90 * S, Screen.height * 0.28f), heading, big);
    }

    void DrawWon(GameManager gm)
    {
        Overlay("CHAPTER COMPLETE", new Color(0.85f, 0.8f, 0.5f));
        small.fontSize = (int)(26 * S);
        small.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        bool last = gm.CurrentChapter >= GameManager.Chapters.Length - 1;
        GUI.Label(Centered(900 * S, 40 * S, Screen.height * 0.28f + 100 * S),
            last ? "You have survived every era of war. Campaign complete."
                 : EraManager.DisplayName(GameManager.Chapters[gm.CurrentChapter + 1]) + " unlocked.",
            small);
        button.fontSize = (int)(26 * S);
        if (GUI.Button(Centered(360 * S, 58 * S, Screen.height * 0.55f), "Continue", button)) gm.EnterMenu();
    }

    void DrawDead(GameManager gm)
    {
        Overlay("YOU DIED", new Color(0.85f, 0.25f, 0.2f));
        button.fontSize = (int)(26 * S);
        if (GUI.Button(Centered(360 * S, 58 * S, Screen.height * 0.5f), "Retry Chapter", button)) gm.RetryChapter();
        if (GUI.Button(Centered(360 * S, 58 * S, Screen.height * 0.5f + 74 * S), "Main Menu", button)) gm.EnterMenu();
    }

    void DrawPaused(GameManager gm)
    {
        Overlay("PAUSED", Color.white);
        button.fontSize = (int)(26 * S);
        if (GUI.Button(Centered(360 * S, 58 * S, Screen.height * 0.48f), "Resume", button)) gm.ResumeFromPause();
        if (GUI.Button(Centered(360 * S, 58 * S, Screen.height * 0.48f + 74 * S), "Restart Chapter", button)) gm.RetryChapter();
        if (GUI.Button(Centered(360 * S, 58 * S, Screen.height * 0.48f + 148 * S), "Main Menu", button)) gm.EnterMenu();
    }
}

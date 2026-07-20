using System.IO;
using UnityEngine;

// GDD section 18 — JSON save (prototype-grade, per the doc).
[System.Serializable]
public class SaveData
{
    public int unlockedChapter;    // index into GameManager.Chapters
    public int chaptersCompleted;
}

public static class SaveSystem
{
    static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "evolution_of_war_save.json"); }
    }

    public static SaveData Load()
    {
        // ponytail: corrupt/missing save falls back to a fresh one silently —
        // add validation + backup per GDD section 18 for production builds.
        try
        {
            if (File.Exists(FilePath))
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                if (data != null) return data;
            }
        }
        catch { }
        return new SaveData();
    }

    public static void Save(SaveData data)
    {
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(data, true)); }
        catch { }
    }
}

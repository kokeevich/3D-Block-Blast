using System;
using UnityEngine;

public static class RunSaveStore
{
    public const string Key="blockblast.run.v1";
    public static RunSnapshot Load()
    {
        if (!PlayerPrefs.HasKey(Key)) return null;
        try
        {
            var snapshot=JsonUtility.FromJson<RunSnapshot>(PlayerPrefs.GetString(Key));
            if (snapshot!=null && snapshot.IsValid()) return snapshot;
        }
        catch (ArgumentException) { }
        // A malformed or unsupported checkpoint must never prevent starting a game.
        Clear(); return null;
    }
    public static void Save(RunSnapshot snapshot)
    {
        if (snapshot==null || !snapshot.IsValid()) return;
        PlayerPrefs.SetString(Key,JsonUtility.ToJson(snapshot)); PlayerPrefs.Save();
    }
    public static void Clear() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
}

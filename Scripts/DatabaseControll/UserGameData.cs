using UnityEngine;
using Firebase.Firestore;
using System.Collections.Generic;

[FirestoreData]
public class UserGameData
{
    [FirestoreProperty] public string nickName { get; set; }

    [FirestoreProperty] public int highScore { get; set; }
    [FirestoreProperty] public int maxCombo { get; set; }
    [FirestoreProperty] public int gold { get; set; }

    [FirestoreProperty] public int totalPlayCount { get; set; }
    [FirestoreProperty] public int totalAdCount { get; set; }

    [FirestoreProperty] public string equippedIconID { get; set; }
    [FirestoreProperty] public List<string> unlockedIconIDs { get; set; }

    [FirestoreProperty] public string equippedThemeID { get; set; }
    [FirestoreProperty] public List<string> unlockedThemeIDs { get; set; }

    [FirestoreProperty] public string equippedCharID { get; set; }
    [FirestoreProperty] public List<string> unlockedCharIDs { get; set; }

    [FirestoreProperty] public bool buttonCase1 { get; set; } = true;
    [FirestoreProperty] public bool frame_60 { get; set; } = true;

    [FirestoreProperty] public Dictionary<string, int> challengeProgress { get; set; }
    [FirestoreProperty] public string lastLoginDate { get; set; }
    [FirestoreProperty] public int currentLoginStreak { get; set; }

    public UserGameData()
    {
        maxCombo = 0;
        totalPlayCount = 0;
        totalAdCount = 0;

        equippedIconID = "Icon_default";
        unlockedIconIDs = new List<string> { "Icon_default" };

        equippedThemeID = "Theme_default";
        unlockedThemeIDs = new List<string> { "Theme_default" };

        equippedCharID = "Char_default";
        unlockedCharIDs = new List<string> { "Char_default" };

        challengeProgress = new Dictionary<string, int>();
        lastLoginDate = "";
        currentLoginStreak = 0;
    }

    public UserGameData(string name, int score) : this()
    {
        nickName = name;
        highScore = score;
        gold = 0;
    }
}
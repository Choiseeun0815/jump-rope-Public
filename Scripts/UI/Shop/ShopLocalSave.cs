using UnityEngine;

public static class ShopLocalSave
{
    private const string KEY = "SHOP_SAVE_V1";

    public static bool HasSave() => PlayerPrefs.HasKey(KEY);

    public static ShopSaveData Load()
    {
        if (!PlayerPrefs.HasKey(KEY)) return new ShopSaveData();

        string json = PlayerPrefs.GetString(KEY, "");
        if (string.IsNullOrEmpty(json)) return new ShopSaveData();

        try
        {
            var data = JsonUtility.FromJson<ShopSaveData>(json);
            return data ?? new ShopSaveData();
        }
        catch
        {
            return new ShopSaveData();
        }
    }

    public static void Save(ShopSaveData data)
    {
        if (data == null) data = new ShopSaveData();
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }
}

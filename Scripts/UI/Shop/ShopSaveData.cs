using System;
using System.Collections.Generic;

[Serializable]
public class ShopSaveData
{
    public int gold = 0;
    public List<string> ownedItemIds = new();
    public string selectedCharacterId = "";
    public string selectedMapId = "";
}

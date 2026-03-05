using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewIconCatalog", menuName = "GameData/Icon Catalog")]
public class IconCatalog : ScriptableObject
{
    [Header("모든 아이콘 데이터 목록")]
    public List<ProfileIconData> allIcons = new List<ProfileIconData>();
}
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Theme", menuName = "Shop/Map Theme")]
public class MapThemeData : ScriptableObject
{
    [Header("ID info")]
    public string themeID; //Desert, Winter 등등... (맵 이름)
    public bool isDefault; //기본으로 제공해주는 맵인지

    [Header("Rope Setting")]
    [ColorUsage(true, true)]
    public Color themeRopeColor = new Color(0, 1, 1, 2);

    [Header("Resources")]
    public GameObject mapPrefab;
    public Sprite leftButtonSprite;
    public Sprite rightButtonSprite;
    public Sprite jumpButtonSprite;
    public AudioClip bgmClip;

    [System.Serializable]
    public struct ThemeObstacleInfo
    {
        public ObstacleType type;
        public GameObject prefab;
    }

    public List<ThemeObstacleInfo> obstacleVisuals;

    public GameObject GetPrefabByType(ObstacleType type)
    {
        foreach (var info in obstacleVisuals)
        {
            if (info.type == type) return info.prefab;
        }
        return null;
    }
}
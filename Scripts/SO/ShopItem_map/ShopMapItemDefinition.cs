using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Map Item Definition")]
public class ShopMapItemDefinition : ShopItemDefinition
{
    [Header("Map Theme Data")]
    public MapThemeData mapThemeData;

    [Header("Lobby Placement (relative to MapPos)")]
    public Vector3 lobbyLocalPosition = Vector3.zero;
    public Vector3 lobbyLocalEuler = Vector3.zero;
    public Vector3 lobbyLocalScale = Vector3.one;

    [Header("Game Placement (relative to MapPos)")]
    public Vector3 gameLocalPosition = Vector3.zero;
    public Vector3 gameLocalEuler = Vector3.zero;
    public Vector3 gameLocalScale = Vector3.one;
}
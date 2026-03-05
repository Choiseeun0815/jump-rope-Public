using UnityEngine;

[CreateAssetMenu(fileName = "New Profile Icon", menuName = "Shop/Profile Icon Data")]
public class ProfileIconData : ScriptableObject
{
    [Header("Basic Info")]
    public string iconID;        
    public Sprite iconSprite;   
    public Color backgroundColor; 

    [Header("Shop Info")]
    public bool isDefault;       // 게임 시작 시 기본으로 주는 아이콘인지 여부
}
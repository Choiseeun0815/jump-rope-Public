using System.Collections.Generic;

[System.Serializable]
public class UserProfileData
{
    public string equippedIconID = "Icon_Default";

    // 내가 해금한 아이콘 ID 목록
    public List<string> unlockedIconIDs = new List<string>();

    public UserProfileData()
    {
        unlockedIconIDs = new List<string>();
        // 기본 아이콘은 무조건 해금 목록에 있어야 함
        unlockedIconIDs.Add("Icon_Default");
    }
}
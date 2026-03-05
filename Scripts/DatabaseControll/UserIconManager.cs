using UnityEngine;

public class UserIconManager : MonoBehaviour
{
    public static UserIconManager Instance;

    [SerializeField] public GameObject BG;
    //public UserProfileData userProfileData;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        CheckAndUnlockDefaultIcons();
    }
    private void Start()
    {
        if(DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
        {
            CheckAndUnlockDefaultIcons();
        }
             
    }
    public void UnlockIcon(string iconID) //캐릭터 획득시 추가 
    {
        UserGameData data = DatabaseManager.Instance.currentData;
        if (data == null) return; //로그인 x 상태

        if (!data.unlockedIconIDs.Contains(iconID))
        {
            data.unlockedIconIDs.Add(iconID);
            DatabaseManager.Instance.UpdateUnlockedIcons();
        }
    }

    public void EquipIcon(string iconID)
    {
        UserGameData data = DatabaseManager.Instance.currentData;
        if (data == null) return;

        if(data.unlockedIconIDs.Contains(iconID))
        {
            DatabaseManager.Instance.UpdateEquippedIcon(iconID);
        }
    }

    public bool IsIconUnlocked(string iconId)
    {
        if (DatabaseManager.Instance.currentData == null) return false;
        return DatabaseManager.Instance.currentData.unlockedIconIDs.Contains(iconId);
    }

    public void CheckAndUnlockDefaultIcons() 
    {
        if (IconManager.Instance == null || DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) return;

        bool isChanged = false;

        foreach (var iconData in IconManager.Instance.GetAllIcons())
        {
            if (iconData.isDefault)
            {
                if (!DatabaseManager.Instance.currentData.unlockedIconIDs.Contains(iconData.iconID))
                {
                    DatabaseManager.Instance.currentData.unlockedIconIDs.Add(iconData.iconID);
                    isChanged = true;
                }
            }
        }

        if (isChanged) DatabaseManager.Instance.UpdateUnlockedIcons();
    }
}

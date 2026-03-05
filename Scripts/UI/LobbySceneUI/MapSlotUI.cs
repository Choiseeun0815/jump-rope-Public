using UnityEngine;

public class MapSlotUI : MonoBehaviour
{
    public MapThemeData data;
    
    public void OnThemeSelectButtonClicked()
    {
        if(DatabaseManager.Instance != null)
        {
            DatabaseManager.Instance.UpdateEquippedTheme(data.themeID);
        }
    }
}

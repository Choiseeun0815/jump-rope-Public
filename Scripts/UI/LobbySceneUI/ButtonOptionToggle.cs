using UnityEngine.UI;
using UnityEngine;

public class ButtonOptionToggle : MonoBehaviour
{
    [SerializeField] Toggle case1Toggle;
    [SerializeField] Toggle case2Toggle;

    [SerializeField] Toggle frame_60;
    [SerializeField] Toggle frame_30;
    private void OnEnable()
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) return;

        UserGameData data = DatabaseManager.Instance.currentData;

        case1Toggle.isOn = data.buttonCase1;
        case2Toggle.isOn = !data.buttonCase1;

        frame_60.isOn = data.frame_60;
        frame_30.isOn = !data.frame_60;
    }

    public void OnToggleChanged(bool isOn)
    {
        if (DatabaseManager.Instance == null) return;
        DatabaseManager.Instance.currentData.buttonCase1 = case1Toggle.isOn;
    }

    public void OnFrameToggleChanged(bool isOn)
    {
        if (DatabaseManager.Instance == null) return;
        bool is60 = frame_60.isOn;
        DatabaseManager.Instance.currentData.frame_60 = is60;

        if(is60)
        {
            Application.targetFrameRate = 60;
        }
        else
        {
            Application.targetFrameRate = 30;
        }
    }

}
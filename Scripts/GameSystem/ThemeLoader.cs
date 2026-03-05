using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

public class ThemeLoader : MonoBehaviour
{
    [SerializeField] private Transform mapParent; // ���� ������ �θ� ������Ʈ
    [SerializeField] private Image case1_leftButton, case1_rightButton, case1_jumpButton;
    [SerializeField] private Image case2_leftButton, case2_rightButton, case2_jumpButton;

    [Header("Fallback Theme ID")]
    [SerializeField] private string fallbackThemeId = "Theme_Default";

    /// <summary>
    /// ? ����: DB �ε���� ��ٷȴٰ� equippedThemeID�� �׸� ����
    /// </summary>
    public async UniTask<MapThemeData> SetupCurrentThemeAsync(CancellationToken ct, float timeoutSeconds = 5f)
    {
        // DB �غ� ���(�ִ� timeoutSeconds)
        await WaitUntilUserDataReadyAsync(timeoutSeconds, ct);

        string currentID = fallbackThemeId;

        if (DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
        {
            string id = DatabaseManager.Instance.currentData.equippedThemeID;
            if (!string.IsNullOrEmpty(id)) currentID = id;
        }

        return ApplyThemeById(currentID);
    }

    private async UniTask WaitUntilUserDataReadyAsync(float timeoutSeconds, CancellationToken ct)
    {
        float start = Time.realtimeSinceStartup;

        while (!ct.IsCancellationRequested)
        {
            if (DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
                return;

            if (Time.realtimeSinceStartup - start >= timeoutSeconds)
            {
                Debug.LogWarning("[ThemeLoader] userData �ε� Ÿ�Ӿƿ� �� fallback �׸��� ����");
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    private MapThemeData ApplyThemeById(string currentID)
    {
        if (MapManager.Instance == null) return null;

        ShopMapItemDefinition data = MapManager.Instance.GetMapItemByID(currentID);

        if (data == null)
        {
            Debug.LogWarning($"data 없음: {currentID}");
            return null;
        }

        if (mapParent == null)
        {
            Debug.LogError("[ThemeLoader] mapParent가 null 임.");
            return null;
        }

        // ���� �� ����
        for (int i = mapParent.childCount - 1; i >= 0; i--)
            Destroy(mapParent.GetChild(i).gameObject);

        // �� ����
        if (data.prefab != null)
        {
            var go = Instantiate(data.prefab, mapParent);
            // �ʿ��ϸ� local �ʱ�ȭ
            go.transform.localPosition = data.gameLocalPosition;
            go.transform.localEulerAngles = data.gameLocalEuler;
            go.transform.localScale = data.gameLocalScale;
        }

        // ��ư �̹��� ��ü
        if (case1_leftButton != null) case1_leftButton.sprite = data.mapThemeData.leftButtonSprite;
        if (case1_rightButton != null) case1_rightButton.sprite = data.mapThemeData.rightButtonSprite;
        if (case1_jumpButton != null) case1_jumpButton.sprite = data.mapThemeData.jumpButtonSprite;

        if (case2_leftButton != null) case2_leftButton.sprite = data.mapThemeData.leftButtonSprite;
        if (case2_rightButton != null) case2_rightButton.sprite = data.mapThemeData.rightButtonSprite;
        if (case2_jumpButton != null) case2_jumpButton.sprite = data.mapThemeData.jumpButtonSprite;

        // BGM ����
        if (BGMSounds.Instance != null && data.mapThemeData.bgmClip != null)
        {
            BGMSounds.Instance.SetGameBGM(data.mapThemeData.bgmClip);
            BGMSounds.Instance.StopBgm();
        }

        return data.mapThemeData;
    }
}
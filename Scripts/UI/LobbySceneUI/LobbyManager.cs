using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class LobbyManager : MonoBehaviour
{
    [Header("UI 갱신용 참조")]
    [SerializeField] private SetGameData setGameData;

    private void Start()
    {
        InitializeLobbySequenceAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid InitializeLobbySequenceAsync(CancellationToken ct)
    {
        while (DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        await UniTask.Yield(PlayerLoopTiming.Update, ct);
        await UniTask.Yield(PlayerLoopTiming.Update, ct);

        if (setGameData != null)
        {
            setGameData.ShowUserInfoButtonClicked();
        }

        System.GC.Collect();

        await UniTask.Delay(300, cancellationToken: ct);

        if (SceneController.Instance != null)
        {
            await SceneController.Instance.FadeIn().ToUniTask(cancellationToken: ct);
        }
    }
}
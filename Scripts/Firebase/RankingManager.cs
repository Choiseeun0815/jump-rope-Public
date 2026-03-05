using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class RankingManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform contentParent; // ScrollView의 Content
    [SerializeField] private GameObject rankItemPrefab; // 랭킹 항목 프리팹
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Settings")]
    [SerializeField] private int maxRankCount = 50;

    [Header("My Rank Display")]
    [SerializeField] private GameObject myRankPanel;
    [SerializeField] private TextMeshProUGUI myRankText;
    [SerializeField] private TextMeshProUGUI myNicknameText;
    [SerializeField] private TextMeshProUGUI myScoreText;
    [SerializeField] private Image topProfileImage;
    [SerializeField] private Image topProfileBG;

    private FirebaseFirestore db;
    private List<GameObject> rankItems = new List<GameObject>();

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        // 시작 시 패널 숨김
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    public void OpenRankingPanel()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);

        UpdateMyInfoImmediate();

        LoadLeaderboard();
    }

    private void UpdateMyInfoImmediate()
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) return;

        UserGameData myData = DatabaseManager.Instance.currentData;

        if (myNicknameText != null) myNicknameText.text = myData.nickName;
        if (myScoreText != null) myScoreText.text = myData.highScore.ToString("N0");

        if (myRankText != null) myRankText.text = "-";

        if (IconManager.Instance != null)
        {
            string iconID = string.IsNullOrEmpty(myData.equippedIconID) ? "Icon_Default" : myData.equippedIconID;

            ProfileIconData iconData = IconManager.Instance.GetIconDataByID(iconID);
            if (iconData != null)
            {
                if (topProfileImage != null) topProfileImage.sprite = iconData.iconSprite;
                if (topProfileBG != null) topProfileBG.color = iconData.backgroundColor;
            }
        }

        // 패널 켜기
        if (myRankPanel != null) myRankPanel.SetActive(true);
    }

    //public void CloseRankingPanel()
    //{
    //    if (leaderboardPanel != null)
    //        leaderboardPanel.SetActive(false);
    //}

    private void LoadLeaderboard()
    {
        ClearRankItems();

        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(true);
            loadingText.text = "Loading...";
        }
        FindMyRank();

        Query query = db.Collection("users")
            .OrderByDescending("highScore")
            .Limit(maxRankCount);

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (loadingText != null)
                loadingText.gameObject.SetActive(false);

            if (task.IsFaulted)
            {
                Debug.LogError("리더보드 로드 실패: " + task.Exception);
                return;
            }

            QuerySnapshot snapshot = task.Result;
            int rank = 1;

            string myUid = DatabaseManager.Instance?.playerUid;

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                UserGameData userData = document.ConvertTo<UserGameData>();

                bool isMe = (document.Id == myUid);

                CreateRankItem(rank, userData, isMe);
                rank++;
            }

        });
    }

    private void CreateRankItem(int rank, UserGameData data, bool isMe)
    {
        if (rankItemPrefab == null || contentParent == null) return;

        GameObject item = Instantiate(rankItemPrefab, contentParent);
        rankItems.Add(item);

        RankItem rankItem = item.GetComponent<RankItem>();
        if (rankItem != null)
        {
            // 아이콘 ID 없으면 디폴트 처리
            string iconIDToUse = string.IsNullOrEmpty(data.equippedIconID) ? "Icon_Default" : data.equippedIconID;

            rankItem.SetData(rank, data.nickName, data.highScore, isMe, iconIDToUse);
        }
    }

    private void ClearRankItems()
    {
        foreach (GameObject item in rankItems)
        {
            if (item != null)
                Destroy(item);
        }
        rankItems.Clear();
    }

    private void FindMyRank()
    {
        if (DatabaseManager.Instance?.currentData == null) return;

        // 닉네임, 점수, 아이콘은 이미 UpdateMyInfoImmediate에서 그렸으므로
        // 여기서는 오직 '등수 계산'만 수행합니다. (중복 갱신 방지)

        int myScore = DatabaseManager.Instance.currentData.highScore;

        // 내 점수보다 높은 사람 수 + 1 = 내 순위
        Query query = db.Collection("users")
            .WhereGreaterThan("highScore", myScore);

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) return;

            int myRank = task.Result.Count + 1;

            if (myRankPanel != null)
            {
                // 등수 텍스트만 쏙 업데이트
                if (myRankText != null) myRankText.text = $"#{myRank}";
            }
        });
    }
}
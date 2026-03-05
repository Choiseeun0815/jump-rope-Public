using UnityEngine;
public enum ChallengeType
{
    AccumulateAds,      //광고 누적
    HighScore,          //최고 점수
    MaxCombo,           //최대 콤보
    PlayCount,          //게임 판수(ex. 100회째 플레이에 해금)
    UnlockMap,          // 특정 맵 해금 (ex. 사막 맵 해금하면 낙타)
    ConsecutiveLogin,   //연속 출석 일수 (ex. 7일 연속 로그인시 달성)
    SpecificMapHighScore, //특정 맵에서 n점 달성
}

[CreateAssetMenu(menuName = "Challenge/Challenge Item Definition")]
public class ChallengeItemDefinition : ShopItemDefinition
{
    [Header("Challenge Config")]
    public ChallengeType type;

    [Tooltip("숫자 목표 (점수, 횟수, 연속출석일 등)")]
    public int targetValue;

    [Tooltip("특정 아이템/맵 해금 조건일 때 그 아이템의 ID (ex. Type: UnlockMap, TargetStringID: map_desert)")]
    public string targetStringID;

    [TextArea]
    public string description;
}
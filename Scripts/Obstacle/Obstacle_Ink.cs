using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Obstacle_Ink : MonoBehaviour
{
    static public Obstacle_Ink Instance;

    [SerializeField] Image[] Ink;
    [SerializeField] float inkShowTime = 1f; //잉크 보여지는 시간
    [SerializeField] float inkHideTime = 2f; //잉크 사라지는 시간 -> 천천히 사라짐
    [SerializeField] float sustainTime = 1.5f; //화면 가림 유지 시간
    [SerializeField] float InkAlpha = 1f; //잉크 투명도

    public int startScore = 5;
    public float inkPercent = .2f; //잉크 방해물 나올 확률

    private bool isInkActive = false;

    private void Awake()
    {
        Instance = this;
    }
    public void InkObstacle()
    {
        if (isInkActive) return;
        if (GameManager.Instance.IsGameOver) return;
        PlayInkSequence();
        //StartCoroutine(InkObstacle_Corutine());
    }

    void PlayInkSequence()
    {
        isInkActive = true;

        Sequence seq = DOTween.Sequence();

        foreach(var item in Ink)
        {
            Color c = item.color;
            c.a = 0;
            item.color = c;

            //seq 타임라인에 예약 걸기(0초 시점에 Fade 효과 실행)
            seq.Insert(0f, item.DOFade(InkAlpha, inkShowTime));

            float fadeOutStartTime = inkShowTime + sustainTime;

            //seq  타임라인에 예약 걸기(잉크 유지 시간이 지나고 fadeout 효과 실행)
            seq.Insert(fadeOutStartTime, item.DOFade(0f, inkHideTime)); 
        }

        seq.OnComplete(() =>
        {
            isInkActive = false;
        });
    }
    
    //기존 잉크 방해물 효과
    IEnumerator InkObstacle_Corutine()
    {
        ShowInk();
        isInkActive = true;

        yield return new WaitForSeconds(inkShowTime + sustainTime);

        HideInk();

        yield return new WaitForSeconds(inkHideTime);

        isInkActive = false;
    }
    public void ShowInk()
    {
        StartCoroutine(Fade(0, InkAlpha, inkShowTime));
    }
    public void HideInk()
    {
        StartCoroutine(Fade(InkAlpha, 0, inkHideTime));
    }
    IEnumerator Fade(float start, float end, float time) //잉크가 점점 나타남
    {
        float currentTime = 0f;
        float percent = 0f;
        
        while(percent<1)
        {
            currentTime += Time.deltaTime;
            percent = currentTime / time;

            foreach(var item in Ink)
            {
                Color color = item.color;
                color.a = Mathf.Lerp(start, end, percent);
                item.color = color;

            }
            yield return null;
        }
    }
   
  
}

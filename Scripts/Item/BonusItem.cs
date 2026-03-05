using UnityEngine;

public class BonusItem : MonoBehaviour
{
    public int value = 5;
    public float lifeTime = 3f; //3초 뒤에는 코인(또는 별) 사라짐
    public float rotateSpeed = 100f;

    public bool isGold = false; //true면 코인, false면 보너스 점수
    private float timer;

    private void OnEnable()
    {
        timer = 0f;
    }
    private void Update()
    {
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);

        timer += Time.deltaTime;
        if(timer>=lifeTime)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if(isGold)
            {
                // 골드 증가
                ScoreManager.Instance.AddGold(value);

                if(EffectSounds.Instance != null)
                    EffectSounds.Instance.CoinSound();
            }
            else
            {
                //점수 증가
                ScoreManager.Instance.AddScore(value);

                if (EffectSounds.Instance != null)
                    EffectSounds.Instance.BonusScoreSound();
            }
            gameObject.SetActive(false);
        }
    }

}
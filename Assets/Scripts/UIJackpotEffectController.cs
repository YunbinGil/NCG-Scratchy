using System.Collections;
using TMPro;
using UnityEngine;

public class UIJackpotEffectController : MonoBehaviour
{
    [Header("연출 & 텍스트")]
    [SerializeField] private GameObject effectRoot;          // UIJackpotEffect 오브젝트
    [SerializeField] private TMP_Text jackpotLabel;          // "잭팟" 문구 텍스트
    [SerializeField] private float showDuration = 2.0f;

    [Header("파티클 시스템")]
    [SerializeField] private ParticleSystem[] jackpotParticles;
    [SerializeField] private int burstCount = 50;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (effectRoot != null)
            effectRoot.SetActive(false);
    }

    /// <summary>
    /// earnedReward가 해당 Lottery의 최고 당첨금(maxReward) 이상일 때 호출
    /// </summary>
    public void TryPlayJackpot(int earnedReward, int maxReward)
    {
        if (maxReward <= 0 || earnedReward < maxReward)
            return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayJackpotRoutine(earnedReward));
    }

    private IEnumerator PlayJackpotRoutine(int earnedReward)
    {
        if (effectRoot != null)
            effectRoot.SetActive(true);

        if (jackpotLabel != null)
            jackpotLabel.text = "잭팟";

        if (jackpotParticles != null)
        {
            foreach (var ps in jackpotParticles)
            {
                if (ps == null) continue;
                ps.Clear();
                ps.Play();
                if (burstCount > 0)
                    ps.Emit(burstCount);
            }
        }

        // 팝인 연출
        Transform targetTransform = jackpotLabel != null ? jackpotLabel.transform : transform;
        float popTime = 0.25f;
        float elapsed = 0f;
        targetTransform.localScale = Vector3.one * 0.3f;

        while (elapsed < popTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popTime);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            targetTransform.localScale = Vector3.LerpUnclamped(Vector3.one * 0.3f, Vector3.one * 1.15f, ease);
            yield return null;
        }
        targetTransform.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(showDuration);

        if (effectRoot != null)
            effectRoot.SetActive(false);
    }
    // ticketMaxReward: 현재 복권(Lottery)의 최고 당첨금액
// earnedReward: 이번 복권에서 나온 당첨금액
// uiJackpotEffectController.TryPlayJackpot(earnedReward, ticketMaxReward);
}
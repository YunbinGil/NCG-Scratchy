using System.Collections.Generic;
using UnityEngine;

public class ScratchBot : MonoBehaviour
{
    [Header("흡입 및 모션 설정")]
    [SerializeField] private float baseAbsorbSpeed = 160f;
    [SerializeField] private float powerSpeedFactor = 4.5f;
    [SerializeField] private float absorbStopDistance = 6f;
    [SerializeField] private float absorbStartOffsetY = 90f;
    [SerializeField] private float absorbEndScale = 0.75f;
    [SerializeField] private Transform holdContainer;

    private readonly List<AbsorbedLotteryEntry> absorbedLotteries = new List<AbsorbedLotteryEntry>();
    private float scratchTimer = 0f;

    private class AbsorbedLotteryEntry
    {
        public GameObject lotteryObject;
        public RectTransform rectTransform;
        public Draggable draggable;
        public IInspectableSource inspectable;
        public Vector3 initialLocalScale;
        public float startDistance;
        public float progress;
        public bool isAbsorbed;
        public bool isScratchedComplete;
    }

    private Transform HoldContainer => holdContainer != null ? holdContainer : transform;

    public int CurrentHeldCount => absorbedLotteries.Count;

    public int MaxCapacity =>
        ScratchBotUpgradeManager.Instance != null
            ? Mathf.Max(1, ScratchBotUpgradeManager.Instance.CurrentMaxCapacity)
            : 1;

    private float CurrentScratchInterval =>
        ScratchBotUpgradeManager.Instance != null
            ? ScratchBotUpgradeManager.Instance.CurrentScratchInterval
            : 0.5f;

    private float CurrentScratchPower =>
        ScratchBotUpgradeManager.Instance != null
            ? ScratchBotUpgradeManager.Instance.CurrentScratchPower
            : 0.15f;

    private float CurrentEffectiveAbsorbSpeed =>
        baseAbsorbSpeed * (1f + Mathf.Max(0f, CurrentScratchPower) * powerSpeedFactor);

    private void Awake()
    {
        if (holdContainer == null)
            holdContainer = transform;
    }

    private void Update()
    {
        UpdateAbsorbingMovement();
        UpdateAutoScratch();
    }

    public bool CanAbsorbMore()
    {
        return CurrentHeldCount < MaxCapacity;
    }

    public bool TryAbsorbLottery(GameObject lotteryObj)
    {
        if (lotteryObj == null || !CanAbsorbMore())
            return false;

        if (ContainsLottery(lotteryObj))
            return false;

        RectTransform rt = lotteryObj.GetComponent<RectTransform>();
        if (rt == null)
            return false;

        Draggable draggable = lotteryObj.GetComponent<Draggable>();
        if (draggable != null)
            draggable.enabled = false;

        Vector3 originalScale = rt.localScale;
        Transform container = HoldContainer;
        lotteryObj.transform.SetParent(container, true);
        rt.SetAsLastSibling();

        Vector3 targetWorldPos = container.position;
        Vector3 startWorldPos = new Vector3(
            rt.position.x,
            Mathf.Max(rt.position.y, targetWorldPos.y + absorbStartOffsetY),
            rt.position.z
        );
        rt.position = startWorldPos;

        float dist = Mathf.Max(0.01f, Vector3.Distance(startWorldPos, targetWorldPos));

        absorbedLotteries.Add(new AbsorbedLotteryEntry
        {
            lotteryObject = lotteryObj,
            rectTransform = rt,
            draggable = draggable,
            inspectable = lotteryObj.GetComponent<IInspectableSource>(),
            initialLocalScale = originalScale,
            startDistance = dist,
            progress = 0f,
            isAbsorbed = false,
            isScratchedComplete = false
        });

        Debug.Log($"ScratchBot 복권 흡수 시작 ({CurrentHeldCount}/{MaxCapacity})");
        return true;
    }

    public bool TryRevealScratchedLottery(GameObject lotteryObj)
    {
        if (lotteryObj == null)
            return false;

        for (int i = 0; i < absorbedLotteries.Count; i++)
        {
            AbsorbedLotteryEntry entry = absorbedLotteries[i];
            if (entry.lotteryObject != lotteryObj)
                continue;

            if (!entry.isScratchedComplete)
                return false;

            IInspectableSource target = entry.inspectable != null
                ? entry.inspectable
                : lotteryObj.GetComponent<IInspectableSource>();

            ReleaseLotterySlot(lotteryObj);

            if (InspectViewManager.Instance != null && target != null)
            {
                InspectViewManager.Instance.OpenInspect(target);

                if (target is Trophy trophy)
                {
                    InspectViewManager.Instance.OnDishCleaned(trophy);
                }
            }

            return true;
        }

        return false;
    }

    public void ReleaseLotterySlot(GameObject lotteryObj)
    {
        for (int i = absorbedLotteries.Count - 1; i >= 0; i--)
        {
            if (absorbedLotteries[i].lotteryObject != lotteryObj)
                continue;

            AbsorbedLotteryEntry entry = absorbedLotteries[i];
            if (entry.rectTransform != null)
                entry.rectTransform.localScale = entry.initialLocalScale;

            if (entry.draggable != null)
                entry.draggable.enabled = true;

            absorbedLotteries.RemoveAt(i);
            break;
        }
    }

    private bool ContainsLottery(GameObject lotteryObj)
    {
        for (int i = 0; i < absorbedLotteries.Count; i++)
        {
            if (absorbedLotteries[i].lotteryObject == lotteryObj)
                return true;
        }
        return false;
    }

    private void UpdateAbsorbingMovement()
    {
        float moveSpeed = CurrentEffectiveAbsorbSpeed;
        Vector3 targetWorldPos = HoldContainer.position;

        for (int i = absorbedLotteries.Count - 1; i >= 0; i--)
        {
            AbsorbedLotteryEntry entry = absorbedLotteries[i];
            if (entry.lotteryObject == null || entry.rectTransform == null)
            {
                absorbedLotteries.RemoveAt(i);
                continue;
            }

            if (entry.isAbsorbed)
                continue;

            entry.rectTransform.position = Vector3.MoveTowards(
                entry.rectTransform.position,
                targetWorldPos,
                moveSpeed * Time.deltaTime
            );

            float remainingDist = Vector3.Distance(entry.rectTransform.position, targetWorldPos);
            float t = 1f - Mathf.Clamp01(remainingDist / entry.startDistance);
            entry.rectTransform.localScale = entry.initialLocalScale * Mathf.Lerp(1f, absorbEndScale, t);

            if (remainingDist <= absorbStopDistance)
            {
                entry.rectTransform.localPosition = Vector3.zero;
                entry.isAbsorbed = true;
            }
        }
    }

    private void UpdateAutoScratch()
    {
        if (!HasPendingScratch())
        {
            scratchTimer = 0f;
            return;
        }

        scratchTimer += Time.deltaTime;
        if (scratchTimer < CurrentScratchInterval)
            return;

        scratchTimer = 0f;
        float power = CurrentScratchPower;

        for (int i = 0; i < absorbedLotteries.Count; i++)
        {
            AbsorbedLotteryEntry entry = absorbedLotteries[i];
            if (entry.lotteryObject == null || entry.isScratchedComplete)
                continue;

            entry.progress = Mathf.Min(1f, entry.progress + power);

            if (entry.progress >= 1f)
            {
                entry.isScratchedComplete = true;
                Debug.Log("봇이 복권을 다 긁었습니다. 복권을 눌러 당첨 결과를 확인하세요.");
            }
        }
    }

    private bool HasPendingScratch()
    {
        for (int i = 0; i < absorbedLotteries.Count; i++)
        {
            AbsorbedLotteryEntry entry = absorbedLotteries[i];
            if (entry.lotteryObject != null && !entry.isScratchedComplete)
                return true;
        }
        return false;
    }
}
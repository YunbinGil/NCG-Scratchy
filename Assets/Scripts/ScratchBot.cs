using System.Collections.Generic;
using UnityEngine;

public class ScratchBot : MonoBehaviour
{
    public static readonly List<ScratchBot> ActiveBots = new List<ScratchBot>();

    [Header("1자 하강 및 긁기 설정")]
    [SerializeField] private float baseAbsorbSpeed = 45f;
    [SerializeField] private float powerSpeedFactor = 1.8f;
    [SerializeField] private float startOffsetY = 30f;
    [SerializeField] private float endOffsetY = -100f;
    [SerializeField] private Transform holdContainer;
    [SerializeField] private Transform lotteryLayer;

    private readonly List<AbsorbedLotteryEntry> absorbedLotteries = new List<AbsorbedLotteryEntry>();

    private class AbsorbedLotteryEntry
    {
        public GameObject lotteryObject;
        public RectTransform rectTransform;
        public Draggable draggable;
        public IInspectableSource inspectable;
        public Transform originalParent;
        public float startY;
        public float targetY;
        public float progress;
        public bool isScratchedComplete;
    }

    private RectTransform BotRectTransform => GetComponent<RectTransform>();

    private Transform BackLayerContainer
    {
        get
        {
            if (lotteryLayer != null)
                return lotteryLayer;

            if (transform.parent != null && transform.parent.parent != null)
            {
                Transform siblingLayer = transform.parent.parent.Find("LottertyLayer");
                if (siblingLayer == null)
                    siblingLayer = transform.parent.parent.Find("LotteryLayer");

                if (siblingLayer != null)
                {
                    lotteryLayer = siblingLayer;
                    return lotteryLayer;
                }
            }

            return holdContainer != null ? holdContainer : transform;
        }
    }

    public int CurrentHeldCount => absorbedLotteries.Count;

    public int MaxCapacity =>
        ScratchBotUpgradeManager.Instance != null
            ? Mathf.Max(1, ScratchBotUpgradeManager.Instance.CurrentMaxCapacity)
            : 1;

    private float CurrentScratchPower =>
        ScratchBotUpgradeManager.Instance != null
            ? Mathf.Max(0.01f, ScratchBotUpgradeManager.Instance.CurrentScratchPower)
            : 0.15f;

    private float CurrentEffectiveAbsorbSpeed =>
        baseAbsorbSpeed * (1f + CurrentScratchPower * powerSpeedFactor);

    private void OnEnable()
    {
        if (!ActiveBots.Contains(this))
            ActiveBots.Add(this);
    }

    private void OnDisable()
    {
        ActiveBots.Remove(this);
    }

    private void Awake()
    {
        if (holdContainer == null)
            holdContainer = transform;

        if (lotteryLayer == null && transform.parent != null && transform.parent.parent != null)
        {
            Transform siblingLayer = transform.parent.parent.Find("LottertyLayer");
            if (siblingLayer == null)
                siblingLayer = transform.parent.parent.Find("LotteryLayer");
            lotteryLayer = siblingLayer;
        }
    }

    private void Update()
    {
        UpdateAbsorbAndScratch();
    }

    public static bool TryRevealFromAnyBot(GameObject lotteryObj)
    {
        if (lotteryObj == null)
            return false;

        for (int i = 0; i < ActiveBots.Count; i++)
        {
            if (ActiveBots[i] != null && ActiveBots[i].TryRevealScratchedLottery(lotteryObj))
                return true;
        }
        return false;
    }

    public bool CanAbsorbMore()
    {
        return CurrentHeldCount < MaxCapacity;
    }

    public bool TryGetScratchProgress(GameObject lotteryObj, out float progress)
    {
        progress = 0f;
        if (lotteryObj == null)
            return false;

        for (int i = 0; i < absorbedLotteries.Count; i++)
        {
            if (absorbedLotteries[i].lotteryObject == lotteryObj)
            {
                progress = absorbedLotteries[i].progress;
                return true;
            }
        }
        return false;
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

        Transform prevParent = lotteryObj.transform.parent;
        Transform targetParent = BackLayerContainer;

        lotteryObj.transform.SetParent(targetParent, true);
        rt.SetAsLastSibling();

        GetClampedVerticalRange(out float fixedX, out float startY, out float targetY, out float fixedZ);
        rt.position = new Vector3(fixedX, startY, fixedZ);

        absorbedLotteries.Add(new AbsorbedLotteryEntry
        {
            lotteryObject = lotteryObj,
            rectTransform = rt,
            draggable = draggable,
            inspectable = lotteryObj.GetComponent<IInspectableSource>(),
            originalParent = prevParent,
            startY = startY,
            targetY = targetY,
            progress = 0f,
            isScratchedComplete = false
        });

        Debug.Log($"ScratchBot 복권 긁는 중 ({CurrentHeldCount}/{MaxCapacity})");
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
            if (entry.originalParent != null && entry.lotteryObject != null)
            {
                entry.lotteryObject.transform.SetParent(entry.originalParent, true);
            }

            if (entry.draggable != null)
                entry.draggable.enabled = true;

            absorbedLotteries.RemoveAt(i);
            break;
        }
    }

    private void GetClampedVerticalRange(out float centerX, out float clampedStartY, out float clampedTargetY, out float zPos)
    {
        RectTransform botRect = BotRectTransform;
        float baseY = transform.position.y;
        zPos = transform.position.z;

        float desiredStartY = baseY + startOffsetY;
        float desiredTargetY = baseY + endOffsetY;

        if (botRect != null && botRect.rect.height > 0.01f)
        {
            Vector3[] corners = new Vector3[4];
            botRect.GetWorldCorners(corners);
            float minBoundY = corners[0].y;
            float maxBoundY = corners[1].y;

            centerX = (corners[0].x + corners[3].x) * 0.5f;
            clampedStartY = Mathf.Clamp(desiredStartY, minBoundY, maxBoundY);
            clampedTargetY = Mathf.Clamp(desiredTargetY, minBoundY, maxBoundY);
            return;
        }

        centerX = transform.position.x;
        clampedStartY = desiredStartY;
        clampedTargetY = desiredTargetY;
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

    private void UpdateAbsorbAndScratch()
    {
        float moveSpeed = CurrentEffectiveAbsorbSpeed;
        GetClampedVerticalRange(out float fixedX, out _, out float targetY, out float fixedZ);

        for (int i = absorbedLotteries.Count - 1; i >= 0; i--)
        {
            AbsorbedLotteryEntry entry = absorbedLotteries[i];
            if (entry.lotteryObject == null || entry.rectTransform == null)
            {
                absorbedLotteries.RemoveAt(i);
                continue;
            }

            if (entry.isScratchedComplete)
            {
                entry.rectTransform.position = new Vector3(fixedX, entry.targetY, fixedZ);
                continue;
            }

            float nextY = Mathf.MoveTowards(
                entry.rectTransform.position.y,
                entry.targetY,
                moveSpeed * Time.deltaTime
            );

            entry.rectTransform.position = new Vector3(fixedX, nextY, fixedZ);

            float totalDist = Mathf.Max(0.01f, Mathf.Abs(entry.startY - entry.targetY));
            float traveledDist = Mathf.Abs(entry.startY - nextY);
            float t = Mathf.Clamp01(traveledDist / totalDist);
            entry.progress = t;

            if (Mathf.Abs(nextY - entry.targetY) <= 1f || t >= 1f)
            {
                entry.rectTransform.position = new Vector3(fixedX, entry.targetY, fixedZ);
                entry.progress = 1f;
                entry.isScratchedComplete = true;
                Debug.Log("봇이 복권을 다 긁었습니다. 복권을 눌러 당첨 결과를 확인하세요.");
            }
        }
    }
}
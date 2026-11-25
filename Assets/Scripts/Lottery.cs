using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Lottery : MonoBehaviour, IInspectableSource
{
    [Header("데이터 (ScriptableObject)")]
    [SerializeField] private LotteryData lotteryData;

    [Header("UI 버튼")]
    [SerializeField] private Button lotteryButton;
    [SerializeField] private Image image;

    [Header("매칭 발광 연출")]
    [SerializeField] private Sprite matchedGlowSprite;
    [SerializeField] private float glowDuration = 0.6f;

    private GameObject stainOverlay;
    private GameObject scratchOverlayRoot;
    private Scratchable[] ownedScratchables;

    public LotteryData Data => lotteryData;

    public int Reward { get; private set; }
    //TODO: rotteryData, rotteryIconData 확률에 따라 결과계산 후 Reward반영

    public bool IsCleaned { get; private set; } = false;
    //TODO: 다 긁어서 결과 나오면 true로 변경
    public List<LotteryIconData> currentLotteryIcons;
    private readonly List<int> currentOpened = new List<int>();
    private readonly HashSet<int> highlightedSlots = new HashSet<int>();
    private Coroutine glowRoutine;
    public void Awake()
    {
        if (lotteryData != null)
        {
            Initialize(lotteryData);
        }
    }
    public void Initialize(LotteryData data)
    {
        lotteryData = data;
        currentOpened.Clear();
        highlightedSlots.Clear();
        Reward = 0;
        IsCleaned = false;

        if (lotteryData == null) return;

        if (image != null && lotteryData.tableSprite != null)
        {
            image.sprite = lotteryData.tableSprite;
        }

        currentLotteryIcons = RollWeightedIcons(lotteryData.availableIcons, lotteryData.scratchCount);
    }
    private static List<LotteryIconData> RollWeightedIcons(List<LotteryIconData> candidates, int count)
    {
        var result = new List<LotteryIconData>(count);
        if (candidates == null || candidates.Count == 0 || count <= 0)
        {
            return result;
        }

        // 유효 아이콘(null 제외, spawnWeight > 0) 필터링 및 총 가중치 계산
        float totalWeight = 0f;
        var validIcons = new List<LotteryIconData>(candidates.Count);

        foreach (var icon in candidates)
        {
            if (icon != null && icon.spawnWeight > 0f)
            {
                validIcons.Add(icon);
                totalWeight += icon.spawnWeight;
            }
        }

        // 가중치가 모두 0 이하일 경우 균등 fallback
        if (totalWeight <= 0f)
        {
            var fallback = new List<LotteryIconData>();
            foreach (var icon in candidates)
            {
                if (icon != null) fallback.Add(icon);
            }

            if (fallback.Count == 0) return result;

            for (int i = 0; i < count; i++)
            {
                result.Add(fallback[Random.Range(0, fallback.Count)]);
            }
            return result;
        }

        for (int i = 0; i < count; i++)
        {
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            LotteryIconData picked = validIcons[validIcons.Count - 1];

            foreach (var icon in validIcons)
            {
                cumulative += icon.spawnWeight;
                if (roll < cumulative)
                {
                    picked = icon;
                    break;
                }
            }

            result.Add(picked);
        }

        return result;
    }

    public void HandleClick()
    {
        if (InspectViewManager.Instance != null && InspectViewManager.Instance.IsInspecting)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {

        }

        InspectViewManager.Instance?.OpenInspect(this);
    }

    public Sprite GetInspectSprite()
    {
        if (lotteryData == null) return null;
        return lotteryData.inspectSprite != null ? lotteryData.inspectSprite : lotteryData.tableSprite;
    }

    public float GetInspectScale()
    {
        return 1f;
    }

    public void OpenInDetailPanel(InspectDetailPanel detailPanel)
    {
        if (detailPanel == null || lotteryData == null) return;
        detailPanel.ShowLottery(lotteryData);
    }

    public void DestoryItem()
    {
        Destroy(gameObject);
    }
    public GameObject GetStainOverlay()
    {
        return stainOverlay;
    }
    public void SetStainOverlay(GameObject overlay)
    {
        stainOverlay = overlay;
        stainOverlay.GetComponent<Scratchable>().CurrentItem = this;
    }
    private void OnDestroy()
    {
        if (stainOverlay != null)
        {
            Destroy(stainOverlay);
            stainOverlay = null;
        }
        if (scratchOverlayRoot != null)
        {
            Destroy(scratchOverlayRoot);
            scratchOverlayRoot = null;
        }
    }

    public void CompleteScratch()
    {
        IsCleaned = true;
    }

    public GameObject GetOrCreateScratchOverlay(Transform parent, Scratchable[] templateScratchables)
    {
        if (scratchOverlayRoot == null && templateScratchables != null && templateScratchables.Length > 0)
        {
            scratchOverlayRoot = new GameObject($"{gameObject.name}_ScratchOverlay");
            if (parent != null)
            {
                scratchOverlayRoot.transform.SetParent(parent, false);
            }

            int count = templateScratchables.Length;
            ownedScratchables = new Scratchable[count];

            for (int i = 0; i < count; i++)
            {
                Scratchable src = templateScratchables[i];
                if (src == null) continue;

                Scratchable clone = Instantiate(src, scratchOverlayRoot.transform);
                clone.name = $"{src.gameObject.name}_{i}";
                clone.transform.localPosition = src.transform.localPosition;
                clone.transform.localRotation = src.transform.localRotation;
                clone.transform.localScale = src.transform.localScale;
                clone.SlotIndex = i;
                clone.CurrentItem = this;
                clone.OnScratchCompleted -= HandleSlotCompleted;
                clone.OnScratchCompleted += HandleSlotCompleted;

                bool isOpened = currentOpened.Contains(i);
                clone.gameObject.SetActive(!isOpened);
                ownedScratchables[i] = clone;
            }
        }

        if (scratchOverlayRoot != null)
        {
            scratchOverlayRoot.SetActive(true);
            RefreshScratchablesActiveState();
        }

        return scratchOverlayRoot;
    }
    public void HideScratchOverlay()
    {
        if (scratchOverlayRoot != null)
        {
            scratchOverlayRoot.SetActive(false);
        }
    }

    public void RefreshScratchablesActiveState()
    {
        if (ownedScratchables == null) return;

        for (int i = 0; i < ownedScratchables.Length; i++)
        {
            if (ownedScratchables[i] == null) continue;
            bool isOpened = currentOpened.Contains(i);
            ownedScratchables[i].gameObject.SetActive(!isOpened);
        }
    }
    private void HandleSlotCompleted(Scratchable slot)
    {
        if (slot == null || lotteryData == null) return;

        int slotIndex = slot.SlotIndex;
        if (!currentOpened.Contains(slotIndex))
        {
            currentOpened.Add(slotIndex);
            slot.gameObject.SetActive(false);
        }

        EvaluateOpenedSlots();

        if (currentOpened.Count >= lotteryData.scratchCount)
        {
            CompleteScratch();
            InspectViewManager.Instance?.OnCleaned(this);
        }
    }
    private void EvaluateOpenedSlots()
    {
        if (lotteryData == null || currentLotteryIcons == null) return;

        var counts = new Dictionary<LotteryIconData, int>();
        var slotsByIcon = new Dictionary<LotteryIconData, List<int>>();

        foreach (int slotIdx in currentOpened)
        {
            if (slotIdx < 0 || slotIdx >= currentLotteryIcons.Count) continue;
            LotteryIconData icon = currentLotteryIcons[slotIdx];
            if (icon == null) continue;

            if (!counts.ContainsKey(icon))
            {
                counts[icon] = 0;
                slotsByIcon[icon] = new List<int>();
            }
            counts[icon]++;
            slotsByIcon[icon].Add(slotIdx);
        }

        int totalReward = 0;
        var newlyMatchedSlots = new List<int>();

        foreach (var pair in counts)
        {
            LotteryIconData icon = pair.Key;
            int matchCount = pair.Value;

            if (matchCount >= lotteryData.requiredMatchCount)
            {
                totalReward += icon.GetPayoutForMatchCount(matchCount);
                foreach (int sIdx in slotsByIcon[icon])
                {
                    if (!highlightedSlots.Contains(sIdx))
                    {
                        highlightedSlots.Add(sIdx);
                        newlyMatchedSlots.Add(sIdx);
                    }
                }
            }
        }

        Reward = totalReward;

        if (newlyMatchedSlots.Count > 0 && matchedGlowSprite != null)
        {
            if (glowRoutine != null) StopCoroutine(glowRoutine);
            glowRoutine = StartCoroutine(FlashMatchedSlotsRoutine(newlyMatchedSlots));
        }
    }
    private IEnumerator FlashMatchedSlotsRoutine(List<int> slotIndices)
    {
        var preview = InspectViewManager.Instance != null
            ? InspectViewManager.Instance.GetComponentInChildren<LotteryPreviewItem>()
            : null;
        GameObject[] iconAreas = preview != null ? preview.GetIconAreas() : null;

        if (iconAreas == null) yield break;

        var originalSprites = new Dictionary<int, Sprite>();
        foreach (int idx in slotIndices)
        {
            if (idx < 0 || idx >= iconAreas.Length || iconAreas[idx] == null) continue;

            var sr = iconAreas[idx].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                originalSprites[idx] = sr.sprite;
                sr.sprite = matchedGlowSprite;
            }
        }

        yield return new WaitForSeconds(glowDuration);

        foreach (var kv in originalSprites)
        {
            if (iconAreas[kv.Key] == null) continue;
            var sr = iconAreas[kv.Key].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = kv.Value;
            }
        }
        glowRoutine = null;
    }
}

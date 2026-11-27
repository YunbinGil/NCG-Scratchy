using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public struct OpenedSymbolChance
{
    public int slotIndex;
    public LotteryIconData symbol;
    public string symbolName;
    public float spawnProbabilityPercent; // 0~100 (%)
    public int openedCount;
    public int payout;
}

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
    private Scratchable[] ownedScratchables;

    public LotteryData Data => lotteryData;

    public int Reward { get; private set; }
    public bool IsJackpot { get; private set; }
    public int JackpotReward { get; set; }
    public bool IsCleaned { get; private set; } = false;
    public bool HasMatched { get; private set; }
    public List<LotteryIconData> currentLotteryIcons;
    private readonly List<int> currentOpened = new List<int>();
    private readonly HashSet<int> highlightedSlots = new HashSet<int>();
    private readonly List<OpenedSymbolChance> lastEvaluatedChances = new List<OpenedSymbolChance>();
    private Coroutine glowRoutine;
    private float rollLuckMultiplier = 1f;
    private GameManager gameManager;
    private UIJackpotEffectController jackpotEffectController;
    private readonly Dictionary<int, IconArea> slotAreasByIndex = new Dictionary<int, IconArea>();

    public IReadOnlyList<OpenedSymbolChance> LastEvaluatedChances => lastEvaluatedChances;

    public void Awake()
    {
        var controller = GameObject.FindWithTag("GameController");
        gameManager = controller != null ? controller.GetComponent<GameManager>() : null;
        jackpotEffectController = controller != null ? controller.GetComponent<UIJackpotEffectController>() : null;
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
        lastEvaluatedChances.Clear();
        Reward = 0;
        IsJackpot = false;
        IsCleaned = false;
        HasMatched = false;

        if (lotteryData == null) return;

        if (image != null && lotteryData.tableSprite != null)
        {
            image.sprite = lotteryData.tableSprite;
        }

        gameManager ??= FindFirstObjectByType<GameManager>();
        rollLuckMultiplier = gameManager != null ? gameManager.LuckMultiplier : 1f;
        currentLotteryIcons = RollWeightedIcons(lotteryData, rollLuckMultiplier);
        Debug.Log("복권 아이콘 랜덤으로 정하기 완료");
        foreach (var icon in currentLotteryIcons)
        {
            Debug.Log($"{icon.iconName}");
        }
        JackpotReward = FindJackpotReward(lotteryData.availableIcons);
    }

    internal void ApplyLuckBeforeInspect()
    {
        if (lotteryData == null || IsCleaned || currentOpened.Count > 0 || gameManager == null)
            return;

        float currentLuck = gameManager.LuckMultiplier;
        if (Mathf.Approximately(currentLuck, rollLuckMultiplier))
            return;

        if (ownedScratchables != null && Array.Exists(ownedScratchables,
                scratchable => scratchable != null && (scratchable.HasScratchMarks || scratchable.IsCompleted)))
            return;

        rollLuckMultiplier = currentLuck;
        currentLotteryIcons = RollWeightedIcons(lotteryData, rollLuckMultiplier);
    }

    private static List<LotteryIconData> RollWeightedIcons(LotteryData data, float luckMultiplier)
    {
        List<LotteryIconData> candidates = data.availableIcons;
        int count = data.scratchCount;
        var result = new List<LotteryIconData>(Mathf.Max(0, count));
        if (candidates == null || candidates.Count == 0 || count <= 0)
        {
            return result;
        }

        // 추첨과 상세 화면에 같은 심볼 가중치를 사용한다.
        float totalWeight = 0f;
        var validIcons = new List<(LotteryIconData Icon, float Weight)>(candidates.Count);

        foreach (var icon in candidates)
        {
            float weight = data.GetSpawnWeight(icon, luckMultiplier);
            if (weight > 0f)
            {
                validIcons.Add((icon, weight));
                totalWeight += weight;
            }
        }

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
                result.Add(fallback[UnityEngine.Random.Range(0, fallback.Count)]);
            }
            return result;
        }

        for (int i = 0; i < count; i++)
        {
            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            LotteryIconData picked = validIcons[validIcons.Count - 1].Icon;

            foreach (var (icon, weight) in validIcons)
            {
                cumulative += weight;
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

    private int FindJackpotReward(List<LotteryIconData> candidates)
    {
        if (candidates == null || candidates.Count == 0) return 0;
        LotteryIconData lastIcon = candidates[candidates.Count - 1];

        if (lastIcon == null || lastIcon.matchPayouts == null || lastIcon.matchPayouts.Length == 0)
            return 0;

        return lastIcon.matchPayouts[lastIcon.matchPayouts.Length - 1];
    }

    public List<OpenedSymbolChance> GetCurrentOpenedSymbolsWithProbability()
    {
        var list = new List<OpenedSymbolChance>();
        if (lotteryData == null || currentLotteryIcons == null)
            return list;

        var counts = new Dictionary<LotteryIconData, int>();

        foreach (int slotIdx in currentOpened)
        {
            if (slotIdx < 0 || slotIdx >= currentLotteryIcons.Count) continue;
            LotteryIconData icon = currentLotteryIcons[slotIdx];
            if (icon == null) continue;

            if (!counts.ContainsKey(icon))
                counts[icon] = 0;
            counts[icon]++;
        }

        foreach (int slotIdx in currentOpened)
        {
            if (slotIdx < 0 || slotIdx >= currentLotteryIcons.Count) continue;
            LotteryIconData icon = currentLotteryIcons[slotIdx];
            if (icon == null) continue;

            int openedCount = counts.TryGetValue(icon, out int c) ? c : 1;
            int payout = openedCount >= lotteryData.requiredMatchCount
                ? icon.GetPayoutForMatchCount(openedCount)
                : 0;

            list.Add(new OpenedSymbolChance
            {
                slotIndex = slotIdx,
                symbol = icon,
                symbolName = string.IsNullOrWhiteSpace(icon.iconName) ? icon.name : icon.iconName,
                spawnProbabilityPercent = lotteryData.GetSymbolProbabilityPercent(icon, rollLuckMultiplier),
                openedCount = openedCount,
                payout = payout
            });
        }

        return list;
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
        detailPanel.ShowLottery(lotteryData, rollLuckMultiplier);
    }

    public void DestoryItem()
    {
        Destroy(gameObject);
    }

    public GameObject GetStainOverlay()
    {
        return stainOverlay;
    }

    public void SetStainOverlay(GameObject overlay, Scratchable[] scratchables)
    {
        stainOverlay = overlay;
        ownedScratchables = scratchables;
        slotAreasByIndex.Clear();

        if (scratchables == null) return;

        foreach (var scratchable in scratchables)
        {
            if (scratchable == null) continue;
            scratchable.CurrentItem = this;

            IconArea area = scratchable.OwnerIconArea != null
                ? scratchable.OwnerIconArea
                : scratchable.GetComponentInParent<IconArea>();

            if (area != null)
            {
                scratchable.OwnerIconArea = area;
                scratchable.SlotIndex = area.SlotIndex;
                slotAreasByIndex[scratchable.SlotIndex] = area;
            }
        }
    }

    private void OnDestroy()
    {
        if (stainOverlay != null)
        {
            Destroy(stainOverlay);
            stainOverlay = null;
        }
    }

    public void CompleteScratch()
    {
        if (IsCleaned)
            return;

        IsCleaned = true;
        // InspectViewManager.Instance?.PlayScratchComplete();
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

    public void OpenAllSlotsByBot()
    {
        if (lotteryData == null) return;

        for (int i = 0; i < lotteryData.scratchCount; i++)
        {
            if (!currentOpened.Contains(i))
            {
                currentOpened.Add(i);
            }
        }

        RefreshScratchablesActiveState();
        EvaluateOpenedSlots(allowJackpotEffect: false);
    }

    public void TriggerJackpotIfNeeded()
    {
        EvaluateOpenedSlots(allowJackpotEffect: false);

        if (!IsJackpot) return;

        if (jackpotEffectController == null)
        {
            GameObject controllerObj = GameObject.FindWithTag("GameController");
            if (controllerObj != null)
                jackpotEffectController = controllerObj.GetComponent<UIJackpotEffectController>();
        }

        if (jackpotEffectController == null)
        {
            Debug.LogWarning("jackpotEffectController가 할당되지 않았습니다.", this);
            return;
        }

        jackpotEffectController.TryPlayJackpot(Reward, JackpotReward);
    }

    public void HandleSlotCompleted(Scratchable slot)
    {
        if (slot == null || lotteryData == null) return;

        int slotIndex = slot.SlotIndex;
        if (!currentOpened.Contains(slotIndex))
        {
            currentOpened.Add(slotIndex);
            slot.gameObject.SetActive(false);
        }

        EvaluateOpenedSlots(allowJackpotEffect: true);

        if (currentOpened.Count >= lotteryData.scratchCount)
        {
            CompleteScratch();
            InspectViewManager.Instance?.OnCleaned(this);
        }
    }

    public void EvaluateOpenedSlots()
    {
        EvaluateOpenedSlots(allowJackpotEffect: true);
    }

    public void EvaluateOpenedSlots(bool allowJackpotEffect)
    {
        if (lotteryData == null || currentLotteryIcons == null) return;

        var openedSymbols = new List<LotteryIconData>();
        var counts = new Dictionary<LotteryIconData, int>();
        var slotsByIcon = new Dictionary<LotteryIconData, List<int>>();

        foreach (int slotIdx in currentOpened)
        {
            Debug.Log($"slotindex {slotIdx}");
            if (slotIdx < 0 || slotIdx >= currentLotteryIcons.Count) continue;
            LotteryIconData icon = currentLotteryIcons[slotIdx];
            if (icon == null) continue;

            openedSymbols.Add(icon);

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
                Debug.Log($"{icon.name} matched: {matchCount}개");
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
        Debug.Log($"Reward: {Reward}");

        lastEvaluatedChances.Clear();
        for (int i = 0; i < currentOpened.Count && i < openedSymbols.Count; i++)
        {
            int slotIdx = currentOpened[i];
            LotteryIconData icon = openedSymbols[i];
            int openedCount = counts.TryGetValue(icon, out int c) ? c : 1;
            int payout = openedCount >= lotteryData.requiredMatchCount
                ? icon.GetPayoutForMatchCount(openedCount)
                : 0;

            lastEvaluatedChances.Add(new OpenedSymbolChance
            {
                slotIndex = slotIdx,
                symbol = icon,
                symbolName = string.IsNullOrWhiteSpace(icon.iconName) ? icon.name : icon.iconName,
                spawnProbabilityPercent = lotteryData.GetSymbolProbabilityPercent(icon, rollLuckMultiplier),
                openedCount = openedCount,
                payout = payout
            });
        }

        ScratchedLotteryClickHandler handler = GetComponent<ScratchedLotteryClickHandler>();
        if (handler == null)
            handler = GetComponentInChildren<ScratchedLotteryClickHandler>();
        if (handler != null)
        {
            handler.SetSymbolProbabilities(lastEvaluatedChances);
        }

        if (newlyMatchedSlots.Count > 0)
        {
            HasMatched = true;
            string keys = slotAreasByIndex.Count == 0
                ? "EMPTY"
                : string.Join(", ", slotAreasByIndex.Keys);
            PlayMatchGlowForSlots(newlyMatchedSlots);
            InspectViewManager.Instance?.OnLotteryMatched(this);
        }

        IsJackpot = Reward > 0 && Reward == JackpotReward;

        if (allowJackpotEffect && IsJackpot)
        {
            if (jackpotEffectController == null)
            {
                Debug.LogWarning("jackpotEffectController가 할당되지 않았습니다.", this);
                return;
            }
            jackpotEffectController.TryPlayJackpot(Reward, JackpotReward);
        }
    }

    private void PlayMatchGlowForSlots(List<int> slotIndices)
    {
        foreach (int slotIdx in slotIndices)
        {
            if (slotAreasByIndex.TryGetValue(slotIdx, out IconArea area) && area != null)
            {
                area.PlayMatchGlow();
            }
        }
    }
}
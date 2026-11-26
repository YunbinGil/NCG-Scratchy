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
    private Scratchable[] ownedScratchables;

    public LotteryData Data => lotteryData;

    public int Reward { get; private set; }
    public bool IsJackpot { get; private set; }
    public int JackpotReward => FindJackpotReward(lotteryData.availableIcons);
    public bool IsCleaned { get; private set; } = false;
    public List<LotteryIconData> currentLotteryIcons;
    private readonly List<int> currentOpened = new List<int>();
    private readonly HashSet<int> highlightedSlots = new HashSet<int>();
    private Coroutine glowRoutine;
    private UIJackpotEffectController jackpotEffectController;
    private readonly Dictionary<int, IconArea> slotAreasByIndex = new Dictionary<int, IconArea>();

    public void Awake()
    {
        jackpotEffectController = GameObject.FindWithTag("GameController").GetComponent<UIJackpotEffectController>();
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
        Debug.Log("복권 아이콘 랜덤으로 정하기 완료");
        foreach (var icon in currentLotteryIcons)
        {
            Debug.Log($"{icon.iconName}");
        }
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
    private int FindJackpotReward(List<LotteryIconData> candidates) //lotteryData.availableIcons 넣을때 확률내림차순으로 넣어야 정상작동
    {
        if (candidates == null || candidates.Count == 0) return 0;
        LotteryIconData lastIcon = candidates[candidates.Count - 1];

        if (lastIcon == null || lastIcon.matchPayouts == null || lastIcon.matchPayouts.Length == 0)
            return 0;

        return lastIcon.matchPayouts[lastIcon.matchPayouts.Length - 1];
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
        IsCleaned = true;
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

    public void HandleSlotCompleted(Scratchable slot)
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
            Debug.Log($"slotindex {slotIdx}");
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

        if (newlyMatchedSlots.Count > 0)
        {
            string keys = slotAreasByIndex.Count == 0
    ? "EMPTY"
    : string.Join(", ", slotAreasByIndex.Keys);
            PlayMatchGlowForSlots(newlyMatchedSlots);
        }

        if (IsJackpot = Reward == JackpotReward)
        {
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
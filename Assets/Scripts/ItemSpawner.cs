using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpawnedTableItemSaveData
{
    public string itemKey; // 프리팹 이름 또는 식별 키 (예: "Lottery01", "Trophy_Gold")
    public string itemKind; // "Lottery" 또는 "Trophy"
    public float posX;
    public float posY;
    public bool isCleaned;
    public int reward;
}

public class ItemSpawner : MonoBehaviour
{
    [Header("일반 아이템")]
    [SerializeField] private List<GameObject> items;

    [Header("세이브 복원용 프리팹 목록 (복권/트로피 프리팹 등록)")]
    [SerializeField] private List<GameObject> savableItemPrefabs = new List<GameObject>();

    [Header("생성 및 드래그 영역")]
    [SerializeField] private RectTransform spawnTable;
    [SerializeField] public RectTransform dragArea;

    [Header("레이어 분리 (복권 아래 / 봇 위)")]
    [SerializeField] private RectTransform lotteryLayer;
    [SerializeField] private RectTransform scratchBotLayer;

    [Header("도감 해금 아이템")]
    [SerializeField] private GameObject unlockPrefab;
    [SerializeField] private Texture2D lightTexture;

    [Header("스크래치 봇")]
    [SerializeField] private GameObject scratchBotPrefab;
    [SerializeField] private Vector2 bottomRightPadding = new Vector2(120f, 120f);

    private readonly List<RectTransform> spawnItems = new List<RectTransform>();
    private readonly List<Vector2> targetpoints = new List<Vector2>();
    private readonly List<GameObject> activeTableItems = new List<GameObject>();

    private float objectSpeed = 600f;

    private GameObject spawnedScratchBot;
    public GameObject SpawnedScratchBot => spawnedScratchBot;
    internal RectTransform TableEffectParent => lotteryLayer != null ? lotteryLayer : spawnTable;

    public bool TrySpawnItem(GameObject item)
    {
        if (spawnTable == null)
            return false;

        float minX = -spawnTable.rect.width / 3f;
        float maxX = spawnTable.rect.width / 3f;
        float minY = 0f;
        float maxY = spawnTable.rect.height / 2f;

        Vector2 targetpoint = new Vector2(
            UnityEngine.Random.Range(minX, maxX),
            UnityEngine.Random.Range(minY, maxY)
        );

        return TrySpawnItem(item, targetpoint, out _);
    }

    public bool TrySpawnItem(
        GameObject item,
        Vector2 targetpoint,
        out GameObject spawnedObject)
    {
        return TrySpawnItem(item, targetpoint, false, out spawnedObject);
    }

    public bool TrySpawnItemAtTableCenter(GameObject item, out GameObject spawnedObject)
    {
        return TrySpawnItem(item, Vector2.zero, true, out spawnedObject);
    }

    private bool TrySpawnItem(
        GameObject item,
        Vector2 targetpoint,
        bool centerOnTable,
        out GameObject spawnedObject)
    {
        return TrySpawnItemInternal(
            item,
            targetpoint,
            centerOnTable,
            lotteryLayer != null ? lotteryLayer : spawnTable,
            out spawnedObject
        );
    }

    private bool TrySpawnItemInternal(
        GameObject item,
        Vector2 targetpoint,
        bool centerOnTable,
        RectTransform targetParent,
        out GameObject spawnedObject)
    {
        spawnedObject = null;

        RectTransform parentRect = targetParent != null ? targetParent : spawnTable;
        if (parentRect == null || item == null ||
            !item.TryGetComponent<RectTransform>(out _))
            return false;

        spawnedObject = Instantiate(
            item,
            transform.position,
            Quaternion.identity,
            parentRect
        );

        GameAudio.RegisterButtons(spawnedObject);

        RectTransform spawnItem =
            spawnedObject.GetComponent<RectTransform>();

        spawnItem.SetAsLastSibling();

        if (centerOnTable)
        {
            Vector2 itemCenter = parentRect.InverseTransformPoint(
                spawnItem.TransformPoint(spawnItem.rect.center));
            targetpoint = spawnItem.anchoredPosition + parentRect.rect.center - itemCenter;
        }

        Draggable draggable =
            spawnedObject.GetComponent<Draggable>();

        if (draggable != null)
        {
            draggable.SetTableRect(dragArea);
        }

        RegisterSpawnedTableItem(spawnedObject);

        spawnItems.Add(spawnItem);
        targetpoints.Add(targetpoint);

        return true;
    }

    private void RegisterSpawnedTableItem(GameObject spawnedObject)
    {
        if (spawnedObject == null) return;
        if (spawnedObject.GetComponent<Lottery>() != null || spawnedObject.GetComponent<Trophy>() != null)
        {
            if (!activeTableItems.Contains(spawnedObject))
                activeTableItems.Add(spawnedObject);
        }
    }

    public List<SpawnedTableItemSaveData> ExportSpawnedTableItems()
    {
        List<SpawnedTableItemSaveData> result = new List<SpawnedTableItemSaveData>();

        for (int i = activeTableItems.Count - 1; i >= 0; i--)
        {
            GameObject go = activeTableItems[i];
            if (go == null || !go)
            {
                activeTableItems.RemoveAt(i);
                continue;
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            Vector2 pos = rt != null ? rt.anchoredPosition : Vector2.zero;

            Lottery lottery = go.GetComponent<Lottery>();
            if (lottery != null)
            {
                string key = lottery.Data != null && lottery.Data.lotteryTablePrefab != null
                    ? lottery.Data.lotteryTablePrefab.name
                    : go.name.Replace("(Clone)", "").Trim();

                result.Add(new SpawnedTableItemSaveData
                {
                    itemKey = key,
                    itemKind = "Lottery",
                    posX = pos.x,
                    posY = pos.y,
                    isCleaned = lottery.IsCleaned,
                    reward = lottery.Reward
                });
                continue;
            }

            Trophy trophy = go.GetComponent<Trophy>();
            if (trophy != null)
            {
                string key = trophy.Data != null
                    ? trophy.Data.name
                    : go.name.Replace("(Clone)", "").Trim();

                result.Add(new SpawnedTableItemSaveData
                {
                    itemKey = key,
                    itemKind = "Trophy",
                    posX = pos.x,
                    posY = pos.y,
                    isCleaned = trophy.IsCleaned,
                    reward = trophy.Reward
                });
            }
        }

        return result;
    }

    public void RestoreSpawnedTableItems(List<SpawnedTableItemSaveData> savedItems)
    {
        for (int i = activeTableItems.Count - 1; i >= 0; i--)
        {
            if (activeTableItems[i] != null && activeTableItems[i])
                Destroy(activeTableItems[i]);
        }
        activeTableItems.Clear();
        spawnItems.Clear();
        targetpoints.Clear();

        if (savedItems == null || savedItems.Count == 0)
            return;

        foreach (SpawnedTableItemSaveData entry in savedItems)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.itemKey))
                continue;

            GameObject prefab = FindSavablePrefab(entry.itemKey);
            if (prefab == null)
            {
                Debug.LogWarning($"[ItemSpawner] 복원할 프리팹을 찾지 못했습니다: {entry.itemKey}", this);
                continue;
            }

            Vector2 savedPos = new Vector2(entry.posX, entry.posY);
            if (TrySpawnItem(prefab, savedPos, out GameObject spawned))
            {
                RectTransform rt = spawned.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = savedPos;
                    int idx = spawnItems.IndexOf(rt);
                    if (idx >= 0 && idx < targetpoints.Count)
                        targetpoints[idx] = savedPos;
                }
            }
        }
    }

    private GameObject FindSavablePrefab(string itemKey)
    {
        if (savableItemPrefabs != null)
        {
            foreach (GameObject prefab in savableItemPrefabs)
            {
                if (prefab == null) continue;
                if (prefab.name == itemKey)
                    return prefab;

                Lottery lottery = prefab.GetComponent<Lottery>();
                if (lottery != null && lottery.Data != null &&
                    lottery.Data.lotteryTablePrefab != null &&
                    lottery.Data.lotteryTablePrefab.name == itemKey)
                    return prefab;

                Trophy trophy = prefab.GetComponent<Trophy>();
                if (trophy != null && trophy.Data != null &&
                    trophy.Data.name == itemKey)
                    return prefab;
            }
        }

        if (items != null)
        {
            foreach (GameObject prefab in items)
            {
                if (prefab != null && prefab.name == itemKey)
                    return prefab;
            }
        }

        return null;
    }

    public bool TrySpawnUnlockItem(System.Action onComplete)
    {
        if (unlockPrefab == null ||
            !unlockPrefab.TryGetComponent<UnlockLotteryCatalog>(out _))
        {
            Debug.LogWarning("해금 프리팹을 확인하세요.", this);
            return false;
        }

        if (!TrySpawnItemAtTableCenter(
            unlockPrefab,
            out GameObject spawnedObject))
        {
            return false;
        }

        UnlockLotteryCatalog unlock =
            spawnedObject.GetComponent<UnlockLotteryCatalog>();

        unlock.Initialize(lightTexture, onComplete);

        return true;
    }

    // 스크래치 봇 테이블 우측 하단 스폰 (프리팹 앵커/피벗/오프셋 유지, 중복 생성 방지)
    public bool TrySpawnScratchBot()
    {
        if (spawnedScratchBot != null)
            return false;

        if (ScratchBotUpgradeManager.Instance != null &&
            !ScratchBotUpgradeManager.Instance.isBotUnlocked)
            return false;

        RectTransform baseRect = scratchBotLayer != null ? scratchBotLayer : spawnTable;
        if (baseRect == null || scratchBotPrefab == null ||
            !scratchBotPrefab.TryGetComponent<RectTransform>(out RectTransform prefabRect))
        {
            Debug.LogWarning("ScratchBot 프리팹 또는 레이어를 확인하세요.", this);
            return false;
        }

        if (baseRect.GetComponentInChildren<ScratchBot>(true) != null)
            return false;

        GameObject botObj = Instantiate(scratchBotPrefab, baseRect);
        GameAudio.RegisterButtons(botObj);
        RectTransform botRect = botObj.GetComponent<RectTransform>();
        if (botRect != null)
        {
            botRect.anchorMin = prefabRect.anchorMin;
            botRect.anchorMax = prefabRect.anchorMax;
            botRect.pivot = prefabRect.pivot;
            botRect.anchoredPosition = prefabRect.anchoredPosition;
            botRect.localScale = prefabRect.localScale;
            botRect.SetAsLastSibling();
        }

        spawnedScratchBot = botObj;
        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (items != null && items.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, items.Count);
                TrySpawnItem(items[randomIndex]);
            }
        }

        for (int i = spawnItems.Count - 1; i >= 0; i--)
        {
            if (spawnItems[i] == null)
            {
                spawnItems.RemoveAt(i);
                targetpoints.RemoveAt(i);
                continue;
            }

            spawnItems[i].anchoredPosition = Vector2.MoveTowards(
                spawnItems[i].anchoredPosition,
                targetpoints[i],
                objectSpeed * Time.deltaTime
            );

            if (Vector2.Distance(
                spawnItems[i].anchoredPosition,
                targetpoints[i]) < 0.01f)
            {
                spawnItems.RemoveAt(i);
                targetpoints.RemoveAt(i);
            }
        }
    }
}
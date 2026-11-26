using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconArea : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    [SerializeField] private SpriteRenderer iconSpriteRenderer;
    [SerializeField] private Scratchable scratchable;
    [SerializeField] private SpriteRenderer backgroundSprite; // 발광 효과를 줄 base background Sprite

    [Header("발광 색상 및 시간")]
    [SerializeField] private Color completeGlowColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Color matchGlowColor = new Color(1f, 0.9f, 0.15f, 0.95f);
    [SerializeField] private float glowDuration = 0.5f;

    private readonly Dictionary<SpriteRenderer, Coroutine> activeGlowRoutines = new Dictionary<SpriteRenderer, Coroutine>();
    private readonly Dictionary<SpriteRenderer, Color> baseColors = new Dictionary<SpriteRenderer, Color>();

    public SpriteRenderer BackgroundSprite => backgroundSprite;
    
    public SpriteRenderer IconSpriteRenderer => iconSpriteRenderer;
    public Scratchable Scratchable => scratchable;
    public int SlotIndex
    {
        get => slotIndex;
        set
        {
            slotIndex = value;
            SyncScratchableReferences();
            if (scratchable != null)
            {
                scratchable.SlotIndex = value;
            }
        }
    }

    private void Awake()
    {
        if (scratchable == null)
            scratchable = GetComponentInChildren<Scratchable>(true);

        CacheBaseColor(backgroundSprite);
        SyncScratchableReferences();
    }
   
    private void OnDisable()
    {
        // 비활성화될 때 발광 코루틴과 색상을 깔끔히 원복
        foreach (var kv in activeGlowRoutines)
        {
            if (kv.Value != null) StopCoroutine(kv.Value);
            if (kv.Key != null && baseColors.TryGetValue(kv.Key, out Color origin))
            {
                kv.Key.color = origin;
            }
        }
        activeGlowRoutines.Clear();
    }
    
    
    private void CacheBaseColor(SpriteRenderer sr)
    {
        if (sr != null && !baseColors.ContainsKey(sr))
        {
            baseColors[sr] = sr.color;
        }
    }
    private void SyncScratchableReferences()
    {
        if (scratchable == null) return;
        scratchable.SlotIndex = slotIndex;
        scratchable.OwnerIconArea = this;
        if (backgroundSprite != null)
        {
            scratchable.BackgroundSprite = backgroundSprite;
        }
    }

    public void SetIconSprite(Sprite sprite)
    {
        if (iconSpriteRenderer == null) return;
        iconSpriteRenderer.sprite = sprite;
        iconSpriteRenderer.enabled = sprite != null;
    }

    public void PlayCompleteGlow()
    {
        PlayGlow(backgroundSprite, completeGlowColor, glowDuration);
    }

    public void PlayMatchGlow()
    {
        PlayGlow(backgroundSprite, matchGlowColor, glowDuration * 1.25f);
    }

    public void PlayGlow(SpriteRenderer targetRenderer, Color glowColor, float duration)
    {
        if (targetRenderer == null || !isActiveAndEnabled || !gameObject.activeInHierarchy) return;

        CacheBaseColor(targetRenderer);

        if (activeGlowRoutines.TryGetValue(targetRenderer, out Coroutine running) && running != null)
        {
            StopCoroutine(running);
        }

        activeGlowRoutines[targetRenderer] = StartCoroutine(GlowRoutine(targetRenderer, glowColor, duration));
    }

    private IEnumerator GlowRoutine(SpriteRenderer targetRenderer, Color glowColor, float duration)
    {
        Color originColor = baseColors[targetRenderer];
        float halfDuration = Mathf.Max(0.01f, duration * 0.5f);
        float elapsed = 0f;

        // 1. 발광 색으로 빠르게 강조
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            targetRenderer.color = Color.Lerp(originColor, glowColor, Mathf.Clamp01(elapsed / halfDuration));
            yield return null;
        }

        // 2. 원래 색상으로 복귀
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            targetRenderer.color = Color.Lerp(glowColor, originColor, Mathf.Clamp01(elapsed / halfDuration));
            yield return null;
        }

        targetRenderer.color = originColor;
        activeGlowRoutines.Remove(targetRenderer);
    }
}
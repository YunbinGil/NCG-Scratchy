using System;
using UnityEngine;

public class Scratchable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer dirtyRenderer;   // 지워질 얼룩 스프라이트
    [SerializeField] private Texture2D brushTexture;         // 지우개 모양 텍스처 (없으면 원형 브러시 사용)

    [Header("Brush & Clean Settings")]
    [SerializeField] private float brushWorldRadius = 0.45f; // 월드 기준 지우개 반지름
    [Range(0.5f, 1f)]
    [SerializeField] private float cleanThreshold = 0.85f;   // 85% 지우면 자동 완료
    [SerializeField] private float checkInterval = 0.1f;
    public SpriteRenderer BackgroundSprite {get; set;} // CheckCompletion()에서 발광효과 줄 base background Sprite
    public IconArea OwnerIconArea { get; set; }

    private Texture2D runtimeTex;
    private Color32[] pixels;
    private int texWidth;
    private int texHeight;
    private int totalInitialOpaquePixels;
    private int erasedPixels;

    private bool isErasing;
    private Vector2 prevWorldPos;
    private float nextCheckTime;
    public bool IsCompleted {get; private set;}
    // 브러시 알파 마스크 캐시 (0 ~ 1)
    private float[] brushMask;
    private int brushMaskSize = 64;
    
    private InspectViewManager inspectViewManager;
    public IInspectableSource CurrentItem {get;set;}
    public int SlotIndex { get; set; }
    public event Action<Scratchable> OnScratchCompleted;

    void Start()
    {
        inspectViewManager = GameObject.FindWithTag("GameController").GetComponent<InspectViewManager>();
        if (targetCamera == null) targetCamera = Camera.main;
        if (dirtyRenderer == null || dirtyRenderer.sprite == null) return;

        BuildBrushMask();
        InitRuntimeTexture();
    }

    void BuildBrushMask()
    {
        brushMask = new float[brushMaskSize * brushMaskSize];

        // brushTexture가 있으면 그 알파값을 사용, 없으면 부드러운 원형 브러시 생성
        Texture2D readableBrush = null;
        if (brushTexture != null && brushTexture.isReadable)
        {
            readableBrush = brushTexture;
        }

        for (int y = 0; y < brushMaskSize; y++)
        {
            for (int x = 0; x < brushMaskSize; x++)
            {
                float u = (x + 0.5f) / brushMaskSize;
                float v = (y + 0.5f) / brushMaskSize;

                float alpha;
                if (readableBrush != null)
                {
                    alpha = readableBrush.GetPixelBilinear(u, v).a;
                }
                else
                {
                    float dx = u * 2f - 1f;
                    float dy = v * 2f - 1f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = dist <= 1f ? Mathf.Clamp01((1f - dist) * 2.5f) : 0f;
                }

                brushMask[y * brushMaskSize + x] = alpha;
            }
        }
    }

    void InitRuntimeTexture()
    {
        Sprite srcSprite = dirtyRenderer.sprite;
        Texture2D srcTex = srcSprite.texture;

        if (!srcTex.isReadable)
        {
            Debug.LogError("얼룩 스프라이트 텍스처의 Read/Write Enabled를 켜주세요!");
            return;
        }

        Rect rect = srcSprite.textureRect;
        texWidth = Mathf.RoundToInt(rect.width);
        texHeight = Mathf.RoundToInt(rect.height);

        runtimeTex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        runtimeTex.filterMode = srcTex.filterMode;
        runtimeTex.wrapMode = TextureWrapMode.Clamp;

        Color[] srcColors = srcTex.GetPixels(
            Mathf.RoundToInt(rect.x),
            Mathf.RoundToInt(rect.y),
            texWidth,
            texHeight
        );

        pixels = new Color32[srcColors.Length];
        totalInitialOpaquePixels = 0;

        for (int i = 0; i < srcColors.Length; i++)
        {
            Color32 c32 = srcColors[i];
            pixels[i] = c32;
            if (c32.a > 20)
            {
                totalInitialOpaquePixels++;
            }
        }

        runtimeTex.SetPixels32(pixels);
        runtimeTex.Apply();

        // 원본 스프라이트와 같은 PPU/피벗으로 새 스프라이트 교체
        Sprite newSprite = Sprite.Create(
            runtimeTex,
            new Rect(0, 0, texWidth, texHeight),
            new Vector2(0.5f, 0.5f),
            srcSprite.pixelsPerUnit
        );
        dirtyRenderer.sprite = newSprite;
    }

    void Update()
    {
        if (IsCompleted || runtimeTex == null) return;

        HandleEraseInput();

        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            CheckCompletion();
        }
    }

    void HandleEraseInput()
    {
        Vector3 mouseWorld3D = targetCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 currentWorld = new Vector2(mouseWorld3D.x, mouseWorld3D.y);

        if (Input.GetMouseButtonDown(0))
        {
            isErasing = true;
            prevWorldPos = currentWorld;
            EraseAtWorldPos(currentWorld);
            runtimeTex.SetPixels32(pixels);
            runtimeTex.Apply();
        }
        else if (Input.GetMouseButton(0) && isErasing)
        {
            float dist = Vector2.Distance(prevWorldPos, currentWorld);
            float stepSize = Mathf.Max(0.03f, brushWorldRadius * 0.25f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / stepSize));

            for (int i = 1; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(prevWorldPos, currentWorld, (float)i / steps);
                EraseAtWorldPos(p);
            }

            runtimeTex.SetPixels32(pixels);
            runtimeTex.Apply();
            prevWorldPos = currentWorld;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isErasing = false;
        }
    }

    void EraseAtWorldPos(Vector2 worldPos)
    {
        Bounds bounds = dirtyRenderer.bounds;
        if (bounds.size.x <= 0.0001f || bounds.size.y <= 0.0001f) return;

        float u = (worldPos.x - bounds.min.x) / bounds.size.x;
        float v = (worldPos.y - bounds.min.y) / bounds.size.y;

        // 브러시 반지름을 텍스처 픽셀 단위로 변환
        float radiusPxX = (brushWorldRadius / bounds.size.x) * texWidth;
        float radiusPxY = (brushWorldRadius / bounds.size.y) * texHeight;
        float radiusPx = Mathf.Max(radiusPxX, radiusPxY);

        int centerX = Mathf.RoundToInt(u * texWidth);
        int centerY = Mathf.RoundToInt(v * texHeight);
        int rInt = Mathf.CeilToInt(radiusPx);

        int minX = Mathf.Max(0, centerX - rInt);
        int maxX = Mathf.Min(texWidth - 1, centerX + rInt);
        int minY = Mathf.Max(0, centerY - rInt);
        int maxY = Mathf.Min(texHeight - 1, centerY + rInt);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float normX = (x - (centerX - rInt)) / Mathf.Max(1f, rInt * 2f);
                float normY = (y - (centerY - rInt)) / Mathf.Max(1f, rInt * 2f);

                int bx = Mathf.Clamp(Mathf.FloorToInt(normX * brushMaskSize), 0, brushMaskSize - 1);
                int by = Mathf.Clamp(Mathf.FloorToInt(normY * brushMaskSize), 0, brushMaskSize - 1);

                float eraseStrength = brushMask[by * brushMaskSize + bx];
                if (eraseStrength <= 0.01f) continue;

                int idx = y * texWidth + x;
                byte oldAlpha = pixels[idx].a;
                if (oldAlpha == 0) continue;

                // 지우개 강도만큼 알파 감소
                byte newAlpha = (byte)Mathf.Max(0, oldAlpha - Mathf.RoundToInt(eraseStrength * 255f));
                pixels[idx].a = newAlpha;

                if (oldAlpha > 20 && newAlpha <= 20)
                {
                    erasedPixels++;
                }
            }
        }
    }

    void CheckCompletion()
    {
        if (totalInitialOpaquePixels <= 0) return;

        float ratio = (float)erasedPixels / totalInitialOpaquePixels;
        if (ratio >= cleanThreshold)
        {
            IsCompleted = true;
            dirtyRenderer.enabled = false;
            Debug.Log($"{this.gameObject.name} 세척 완료! ({ratio * 100f:F1}%)");

            if (OwnerIconArea != null)
            {
                OwnerIconArea.PlayCompleteGlow();
            }
            else if (BackgroundSprite != null)
            {
                BackgroundSprite.color = Color.white;
            }

            if (CurrentItem is Lottery currentLottery)
            {
                currentLottery.HandleSlotCompleted(this);
            }
            else
            {
                CurrentItem?.CompleteScratch();
                inspectViewManager?.OnCleaned(CurrentItem);
            }
        }
    }


    void OnDestroy()
    {
        if (runtimeTex != null) Destroy(runtimeTex);
    }
}
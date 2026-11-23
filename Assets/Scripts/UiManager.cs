using UnityEngine;

public class UiManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    public Camera targetCamera;
    public SpriteRenderer cursorRenderer; 
    public Sprite defaultCursorSprite;   
    public Sprite scrubberSprite;       
    public Sprite scrubCoinSprite;       
    

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        SetDefaultCursor();
    }

    void Update()
    {
        FollowMouse();
    }

    void FollowMouse()
    {
        if (cursorRenderer == null) return;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 worldPos = targetCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = cursorRenderer.transform.position.z;
        cursorRenderer.transform.position = worldPos;
    }

    public void SetCursorSprite(Sprite newSprite)
    {
        if (cursorRenderer != null && newSprite != null)
        {
            cursorRenderer.sprite = newSprite;
        }
    }

    public void SetDefaultCursor()
    {
        SetCursorSprite(defaultCursorSprite);
    }

    public void SetScrubberCursor()
    {
        SetCursorSprite(scrubberSprite);
    }
    public void SetScrubCoinCursor()
    {
        SetCursorSprite(scrubCoinSprite);
    }
}
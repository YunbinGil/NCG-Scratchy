using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UnlockLotteryCatalog : MonoBehaviour
{
    private Button button;
    private Texture2D lightTexture;
    private Action onComplete;
    private string revealMessage;
    private string revealLocalizationKey;
    private Image glow;
    private Sprite glowSprite;
    private TextMeshProUGUI revealLabel;

    public void Initialize(
        Texture2D texture, Action complete, string message = "Catalog Unlocked!", string localizationKey = null)
    {
        lightTexture = texture;
        onComplete = complete;
        revealMessage = message;
        revealLocalizationKey = localizationKey ?? (message == "Upgrades Unlocked!"
            ? "unlock.upgrades.complete"
            : "unlock.catalog.complete");
        button = GetComponent<Button>();
        button.onClick.AddListener(BeginReveal);

        TMP_Text label = GetComponentInChildren<TMP_Text>();
        if (label != null)
            Localization.Bind(label, "unlock.interact", "Unlock");
    }

    private void BeginReveal()
    {
        button.interactable = false;
        StartCoroutine(Reveal());
    }

    private IEnumerator Reveal()
    {
        RectTransform item = (RectTransform)transform;
        Vector3 originalScale = item.localScale;
        Vector3 smallScale = originalScale * 0.05f;

        yield return ScaleItem(item, originalScale, smallScale, 0.15f);
        CreateGlow(item);
        CreateRevealLabel(item);
        yield return ScaleItem(item, smallScale, originalScale, 0.35f);
        yield return new WaitForSecondsRealtime(0.75f);

        onComplete?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator ScaleItem(RectTransform item, Vector3 from, Vector3 to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            item.localScale = Vector3.LerpUnclamped(from, to, progress);
            yield return null;
        }

        item.localScale = to;
    }

    private void CreateGlow(RectTransform item)
    {
        if (lightTexture == null)
            return;

        glowSprite = Sprite.Create(lightTexture, new Rect(0, 0, lightTexture.width, lightTexture.height),
            new Vector2(0.5f, 0.5f));

        GameObject glowObject = new GameObject("Catalog Unlock Light", typeof(RectTransform), typeof(Image));
        glow = glowObject.GetComponent<Image>();
        glow.sprite = glowSprite;
        glow.raycastTarget = false;
        glow.preserveAspect = true;

        RectTransform glowRect = glow.rectTransform;
        glowRect.SetParent(item.parent, false);
        glowRect.SetSiblingIndex(item.GetSiblingIndex());
        float size = Mathf.Max(item.rect.width, item.rect.height) * 2f;
        glowRect.sizeDelta = new Vector2(size, size);
        UpdateGlowPosition();
    }

    private void CreateRevealLabel(RectTransform item)
    {
        GameObject labelObject = new GameObject("Catalog Unlock Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        revealLabel = labelObject.GetComponent<TextMeshProUGUI>();
        Localization.Bind(revealLabel, revealLocalizationKey, revealMessage);
        revealLabel.alignment = TextAlignmentOptions.Center;
        revealLabel.fontSize = 30f;
        revealLabel.fontStyle = FontStyles.Bold;
        revealLabel.color = new Color32(89, 53, 20, 255);
        revealLabel.raycastTarget = false;
        revealLabel.rectTransform.SetParent(item.parent, false);
        revealLabel.rectTransform.sizeDelta = new Vector2(360f, 48f);
        UpdateRevealLabelPosition();
    }

    private void LateUpdate()
    {
        if (glow != null)
            UpdateGlowPosition();
        if (revealLabel != null)
            UpdateRevealLabelPosition();
    }

    private void UpdateGlowPosition()
    {
        RectTransform item = (RectTransform)transform;
        glow.rectTransform.position = item.TransformPoint(item.rect.center);
    }

    private void UpdateRevealLabelPosition()
    {
        RectTransform item = (RectTransform)transform;
        Vector3 bottom = item.parent.InverseTransformPoint(item.TransformPoint(item.rect.center.x, item.rect.yMin, 0f));
        revealLabel.rectTransform.localPosition = bottom + Vector3.down * 26f;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(BeginReveal);
        if (glow != null)
            Destroy(glow.gameObject);
        if (revealLabel != null)
            Destroy(revealLabel.gameObject);
        if (glowSprite != null)
            Destroy(glowSprite);
    }
}

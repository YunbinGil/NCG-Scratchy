using TMPro;
using UnityEngine;

public sealed class LocalizedLabel : MonoBehaviour
{
    private TMP_Text label;
    [SerializeField] private string key;
    private string fallback;
    private object[] arguments = System.Array.Empty<object>();

    internal void Set(TMP_Text target, string textKey, string fallbackText, object[] formatArguments)
    {
        label = target;
        key = textKey;
        fallback = fallbackText;
        arguments = formatArguments;
        Refresh();
    }

    private void OnEnable()
    {
        if (label == null && !string.IsNullOrWhiteSpace(key))
        {
            label = GetComponent<TMP_Text>();
            fallback = label != null ? label.text : key;
            if (string.IsNullOrEmpty(fallback))
                fallback = key;
        }

        Localization.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Localization.LanguageChanged -= Refresh;
    }

    private void Refresh()
    {
        if (label != null)
            label.text = key == null ? fallback : Localization.Get(key, fallback, arguments);
    }
}

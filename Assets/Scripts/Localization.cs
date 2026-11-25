using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Localization
{
    private const string LanguagePreference = "game.language";
    private static readonly Dictionary<string, Dictionary<string, string>> tables =
        new Dictionary<string, Dictionary<string, string>>();

    private static readonly Dictionary<string, string> authoredLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "CONTINUE", "menu.continue" },
            { "MAIN MENU", "menu.main_menu" },
            { "EXIT", "menu.exit" },
            { "SETTINGS", "menu.settings" },
            { "PLAY", "menu.play" },
            { "Work", "work" },
            { "Lottery", "lottery" },
            { "Machine", "machine" },
            { "Upgrade", "upgrade" },
            { "Lock", "locked" }
        };

    public static event Action LanguageChanged;
    public static string CurrentLanguage { get; private set; } = GetSavedLanguage();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        tables.Clear();
        LanguageChanged = null;
        CurrentLanguage = GetSavedLanguage();
        SceneManager.sceneLoaded -= TranslateScene;
        SceneManager.sceneLoaded += TranslateScene;
    }

    private static string GetSavedLanguage()
    {
        return PlayerPrefs.GetString(LanguagePreference, "en") == "ko" ? "ko" : "en";
    }

    public static void SetLanguage(string language)
    {
        if ((language != "ko" && language != "en") || language == CurrentLanguage)
            return;

        CurrentLanguage = language;
        PlayerPrefs.SetString(LanguagePreference, language);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public static string Get(string key, string fallback, params object[] arguments)
    {
        if (!GetTable(CurrentLanguage).TryGetValue(key, out string value) &&
            !GetTable("en").TryGetValue(key, out value))
            value = fallback;

        return arguments.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, arguments);
    }

    public static void Bind(TMP_Text label, string key, string fallback, params object[] arguments)
    {
        if (label == null)
            return;

        LocalizedLabel binding = label.GetComponent<LocalizedLabel>();
        if (binding == null)
            binding = label.gameObject.AddComponent<LocalizedLabel>();

        binding.Set(label, key, fallback, arguments);
    }

    public static void Clear(TMP_Text label, string text)
    {
        if (label == null)
            return;

        LocalizedLabel binding = label.GetComponent<LocalizedLabel>();
        if (binding != null)
            binding.Set(label, null, text, Array.Empty<object>());
        else
            label.text = text;
    }

    public static void BindAuthoredLabels(GameObject root)
    {
        foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.GetComponent<LocalizedLabel>() == null &&
                authoredLabels.TryGetValue((label.text ?? string.Empty).Trim(), out string key))
                Bind(label, key, label.text);
        }
    }

    private static void TranslateScene(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            BindAuthoredLabels(root);
    }

    private static Dictionary<string, string> GetTable(string language)
    {
        if (tables.TryGetValue(language, out Dictionary<string, string> table))
            return table;

        table = new Dictionary<string, string>(StringComparer.Ordinal);
        TextAsset csv = Resources.Load<TextAsset>($"Localization/StringTable.{language}");
        if (csv != null)
        {
            foreach (string line in csv.text.TrimStart('\uFEFF').Split('\n'))
            {
                string[] columns = line.TrimEnd('\r').Split(new[] { ',' }, 2);
                if (columns.Length != 2)
                    continue;

                string key = columns[0].Trim().Trim('"');
                if (key.Length == 0 || key == "Key")
                    continue;

                string value = columns[1];
                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                    value = value.Substring(1, value.Length - 2).Replace("\"\"", "\"");

                table[key] = value;
            }
        }

        tables.Add(language, table);
        return table;
    }
}

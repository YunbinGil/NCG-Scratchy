using UnityEditor;

internal static class LocalizationMenu
{
    private const string KoreanMenu = "Language/한국어";
    private const string EnglishMenu = "Language/English";

    [MenuItem(KoreanMenu, false, 10)]
    private static void SelectKorean()
    {
        Localization.SetLanguage("ko");
    }

    [MenuItem(EnglishMenu, false, 11)]
    private static void SelectEnglish()
    {
        Localization.SetLanguage("en");
    }

    [MenuItem(KoreanMenu, true)]
    private static bool ValidateKorean()
    {
        Menu.SetChecked(KoreanMenu, Localization.CurrentLanguage == "ko");
        return true;
    }

    [MenuItem(EnglishMenu, true)]
    private static bool ValidateEnglish()
    {
        Menu.SetChecked(EnglishMenu, Localization.CurrentLanguage == "en");
        return true;
    }
}

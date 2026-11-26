using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnlockEffect))]
public class UpgradeUnlock : MonoBehaviour
{
    public static UpgradeUnlock Instance { get; private set; }

    private const string UnlockedPrefKey = "UpgradePanel_IsUnlocked";

    private UnlockEffect effect;
    private GameManager gameManager;
    private UnlockEffect.Registration registration;

    public bool IsUnlocked { get; private set; }

    internal static void ClearSavedUnlockState()
    {
        PlayerPrefs.DeleteKey(UnlockedPrefKey);
    }

    internal void Initialize(GameManager player)
    {
        Instance = this;
        gameManager = player;
        effect = GetComponent<UnlockEffect>();
        IsUnlocked = false;
        UpdatePanelVisibility();
        RegisterUnlock();
    }

    public void ApplySavedUnlockState(bool unlocked)
    {
        registration?.Cancel();
        registration = null;

        IsUnlocked = unlocked && gameManager != null && effect.MeetsLevel(gameManager.Level);
        PlayerPrefs.SetInt(UnlockedPrefKey, IsUnlocked ? 1 : 0);
        PlayerPrefs.Save();
        UpdatePanelVisibility();
        RegisterUnlock();
    }

    public void ResetUnlockStateForNewGame()
    {
        ApplySavedUnlockState(false);
    }

    private void RegisterUnlock()
    {
        if (IsUnlocked || gameManager == null || registration != null)
            return;

        registration = gameManager.UnlockEffects.Watch(this, effect, CompleteUnlock,
            completeAfterVisual: true);
    }

    private void CompleteUnlock()
    {
        registration = null;
        ApplySavedUnlockState(true);
    }

    private void UpdatePanelVisibility()
    {
        if (IsUnlocked)
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(true);
        }

        gameObject.SetActive(IsUnlocked);
    }


    private void OnDestroy()
    {
        registration?.Cancel();

        if (Instance == this)
            Instance = null;
    }
}

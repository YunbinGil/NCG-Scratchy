using System;
using System.Collections.Generic;
using UnityEngine;

public enum PhoneCallEventType
{
    FirstLossOrBreak,
    Level3Reached,
    Level4Reached,
    Level5Reached,
    Level6Reached,
    Level7Reached,
    EndingGoalReached
}

[Serializable]
public class PhoneCallScriptEntry
{
    public string entryName;
    public PhoneCallEventType eventType;
    public bool triggerOnlyOnce = true;

    public string[] lineKeys;
}

[Serializable]
public class PhoneEventSaveData
{
    public List<PhoneCallEventType> triggeredEvents = new List<PhoneCallEventType>();
    public bool hasPendingCall;
    public PhoneCallEventType pendingEventType;
    public string[] pendingLines;
}

public class PhoneEventController : MonoBehaviour
{
    public static PhoneEventController Instance { get; private set; }

    [Header("전화기 트리거")]
    [SerializeField] private PhoneCallTrigger phoneCallTrigger;

    [Header("이벤트별 전화 대사 목록")]
    [SerializeField] private List<PhoneCallScriptEntry> callEntries = new List<PhoneCallScriptEntry>
    {
        new PhoneCallScriptEntry
        {            
            eventType = PhoneCallEventType.FirstLossOrBreak,
            triggerOnlyOnce = true,
            lineKeys = new[] { "phone.string.01", "phone.string.02" }
        },
        new PhoneCallScriptEntry
        {
            entryName = "LEVEL 3",
            eventType = PhoneCallEventType.Level3Reached,
            triggerOnlyOnce = true,
            lineKeys = new[] { "phone.string.03", "phone.string.04", "phone.string.05" }
        },
        new PhoneCallScriptEntry
        {
            entryName = "LEVEL 4",
            eventType = PhoneCallEventType.Level4Reached,
            triggerOnlyOnce = true,
            lineKeys = new[] { "phone.string.06", "phone.string.07", "phone.string.08", "phone.string.09", "phone.string.10" }
        },
        new PhoneCallScriptEntry
        {
            entryName = "LEVEL 5",
            eventType = PhoneCallEventType.Level5Reached,
            triggerOnlyOnce = true,
            lineKeys = new[] { "phone.string.11", "phone.string.12", "phone.string.13" }
        },
        new PhoneCallScriptEntry
        {
            entryName = "LEVEL 6",
            eventType = PhoneCallEventType.Level6Reached,
            triggerOnlyOnce = true,
            lineKeys = new[] { "phone.string.14" }
        },
        new PhoneCallScriptEntry
        {
            entryName = "LEVEL 7",
            eventType = PhoneCallEventType.Level7Reached,
            triggerOnlyOnce = true,
            lineKeys = new[] { "phone.string.15", "phone.string.16", "phone.string.17" }
        }
    };

    private readonly HashSet<PhoneCallEventType> _triggeredEvents = new HashSet<PhoneCallEventType>();
    private bool _hasPendingCall;
    private PhoneCallEventType _pendingEventType;
    private string[] _pendingLegacyLines;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (phoneCallTrigger == null)
            phoneCallTrigger = FindFirstObjectByType<PhoneCallTrigger>();
    }

    private bool endingCallStarted;

    public bool TryRingEndingCall(out PhoneCallTrigger phone)
    {
        phone = phoneCallTrigger;
        if (endingCallStarted || phone == null || !phone.isActiveAndEnabled)
            return false;

        endingCallStarted = true;
        _hasPendingCall = true;
        _pendingEventType = PhoneCallEventType.EndingGoalReached;
        _pendingLines = null;
        phone.RingPhone();
        return true;
    }

    public void TriggerEvent(PhoneCallEventType eventType)
    {
        if (endingCallStarted)
            return;

        if (phoneCallTrigger == null)
            phoneCallTrigger = FindFirstObjectByType<PhoneCallTrigger>();

        if (phoneCallTrigger == null) return;

        PhoneCallScriptEntry entry = callEntries.Find(e => e.eventType == eventType);
        if (entry?.lineKeys == null || entry.lineKeys.Length == 0) return;

        if (entry.triggerOnlyOnce && _triggeredEvents.Contains(eventType))
            return;

        if (entry.triggerOnlyOnce)
            _triggeredEvents.Add(eventType);

        _hasPendingCall = true;
        _pendingEventType = eventType;
        _pendingLegacyLines = null;

        phoneCallTrigger.RingPhone(entry.lineKeys);
    }

    public void NotifyCallAnswered()
    {
        _hasPendingCall = false;
        _pendingLegacyLines = null;
    }

    public void CheckLevelEvents(int currentLevel)
    {
        if (currentLevel >= 3) TriggerEvent(PhoneCallEventType.Level3Reached);
        if (currentLevel >= 4) TriggerEvent(PhoneCallEventType.Level4Reached);
        if (currentLevel >= 5) TriggerEvent(PhoneCallEventType.Level5Reached);
        if (currentLevel >= 6) TriggerEvent(PhoneCallEventType.Level6Reached);
        if (currentLevel >= 7) TriggerEvent(PhoneCallEventType.Level7Reached);
    }

    public void TriggerBrokenOrLossCall()
    {
        TriggerEvent(PhoneCallEventType.FirstLossOrBreak);
    }

    public PhoneEventSaveData ExportSaveData()
    {
        if (phoneCallTrigger == null)
            phoneCallTrigger = FindFirstObjectByType<PhoneCallTrigger>();

        bool isRingingNow = phoneCallTrigger != null && phoneCallTrigger.IsCurrentlyRinging;
        return new PhoneEventSaveData
        {
            triggeredEvents = new List<PhoneCallEventType>(_triggeredEvents),
            hasPendingCall = isRingingNow || _hasPendingCall,
            pendingEventType = _pendingEventType,
            pendingLines = _hasPendingCall ? _pendingLegacyLines : null
        };
    }

    public void RestoreSaveData(PhoneEventSaveData saveData)
    {
        if (phoneCallTrigger == null)
            phoneCallTrigger = FindFirstObjectByType<PhoneCallTrigger>();

        _triggeredEvents.Clear();
        if (saveData != null && saveData.triggeredEvents != null)
        {
            foreach (PhoneCallEventType ev in saveData.triggeredEvents)
                _triggeredEvents.Add(ev);
        }

        if (saveData != null && saveData.hasPendingCall)
        {
            PhoneCallScriptEntry entry = callEntries.Find(e => e.eventType == saveData.pendingEventType);
            string[] lineKeys = entry?.lineKeys;
            _pendingLegacyLines = lineKeys == null || lineKeys.Length == 0 ? saveData.pendingLines : null;
            _hasPendingCall = true;
            _pendingEventType = saveData.pendingEventType;

            if (phoneCallTrigger != null)
                phoneCallTrigger.RingPhone(lineKeys, _pendingLegacyLines);
        }
        else
        {
            _hasPendingCall = false;
            _pendingLegacyLines = null;

            if (phoneCallTrigger != null)
                phoneCallTrigger.StopRinging();
        }
    }
}
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

    [TextArea(2, 4)]
    public string[] lines;
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
            lines = new[]
            {
                "You can't win every time. You might occasionally incur a loss",
                "But don't worry. Just throw it in the trash."
            }
        },
        new PhoneCallScriptEntry
        {
            entryName = "LEVEL 3",
            eventType = PhoneCallEventType.Level3Reached,
            triggerOnlyOnce = true,
            lines = new[]
            {
                "Do you feel like your main job is boring?",
                "Then there's one that fits perfectly...",
                "It's the scratch ticket!"
            }
        },
        new PhoneCallScriptEntry
        {
            entryName = "LEVEL 4",
            eventType = PhoneCallEventType.Level4Reached,
            triggerOnlyOnce = true,
            lines = new[]
            {
                "It would take quite a while to scratch each one, right?",
                "Don't worry, I've prepared what you need. It's an upgrade!",
                "Upgrading the scratch tool makes it much easier to draw!",
                "You'll need more force to scratch a harder coating later..",
                "You can even upgrade your luck. It's amazing, isn't it!"
            }
        },
        new PhoneCallScriptEntry
        {
            entryName = "LEVEL 5",
            eventType = PhoneCallEventType.Level5Reached,
            triggerOnlyOnce = true,
            lines = new[]
            {
                "Wow, the new tickets are ready!",
                "This ticket's reward is much better, but..",
                "To win, you'll need a bit more luck, though"
            }
        },
        new PhoneCallScriptEntry
        {
            entryName = "LEVEL 6",
            eventType = PhoneCallEventType.Level6Reached,
            triggerOnlyOnce = true,
            lines = new[]
            {
                "Wow, you've accumulated a lot of tickets! Don't you feel a bit tired from scratching them all by hand?"
            }
        },
        new PhoneCallScriptEntry
        {
            entryName = "LEVEL 7",
            eventType = PhoneCallEventType.Level7Reached,
            triggerOnlyOnce = true,
            lines = new[]
            {
                "Be careful of bugs when scraping an apple tree!",
                "If you see a bug, never scratch it!",
                "Even if a bug appears, a loss only occurs once it has completely scratched the item."
            }
        }
    };

    private readonly HashSet<PhoneCallEventType> _triggeredEvents = new HashSet<PhoneCallEventType>();
    private bool _hasPendingCall;
    private PhoneCallEventType _pendingEventType;
    private string[] _pendingLines;

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
        if (entry == null || entry.lines == null || entry.lines.Length == 0) return;

        if (entry.triggerOnlyOnce && _triggeredEvents.Contains(eventType))
            return;

        if (entry.triggerOnlyOnce)
            _triggeredEvents.Add(eventType);

        _hasPendingCall = true;
        _pendingEventType = eventType;
        _pendingLines = entry.lines;

        phoneCallTrigger.RingPhone(entry.lines);
    }

    public void NotifyCallAnswered()
    {
        _hasPendingCall = false;
        _pendingLines = null;
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
        string[] linesNow = isRingingNow && phoneCallTrigger.CurrentQueuedLines != null
            ? (string[])phoneCallTrigger.CurrentQueuedLines.Clone()
            : (_hasPendingCall ? _pendingLines : null);

        return new PhoneEventSaveData
        {
            triggeredEvents = new List<PhoneCallEventType>(_triggeredEvents),
            hasPendingCall = isRingingNow || _hasPendingCall,
            pendingEventType = _pendingEventType,
            pendingLines = linesNow
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
            string[] linesToRing = saveData.pendingLines;
            if (linesToRing == null || linesToRing.Length == 0)
            {
                PhoneCallScriptEntry entry = callEntries.Find(e => e.eventType == saveData.pendingEventType);
                if (entry != null)
                    linesToRing = entry.lines;
            }

            _hasPendingCall = true;
            _pendingEventType = saveData.pendingEventType;
            _pendingLines = linesToRing;

            if (phoneCallTrigger != null)
                phoneCallTrigger.RestoreRingingState(true, linesToRing);
        }
        else
        {
            _hasPendingCall = false;
            _pendingLines = null;

            if (phoneCallTrigger != null)
                phoneCallTrigger.RestoreRingingState(false, null);
        }
    }
}
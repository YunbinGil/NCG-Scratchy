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
    Level7Reached
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
                // "매번 딸 수는 없는 법이죠. 가끔은 손해를 볼 수도 있을 거예요.",
                // "그래도 걱정 마세요. 그냥 쓰레기통에 버리면 되니까."

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
                // "본업이 지겹다고 느껴지시나요?"
                // "그렇다면 딱 맞는 게 있는데.."
                // "바로 스크래치 티켓이랍니다!"

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
                // "그거 일일이 긁으려니 시간이 꽤 걸리죠?"
                // "걱정 마세요, 당신에게 필요한 걸 준비했으니까요. 바로 업그레이드죠!"
                // "스크래치 도구를 업그레이드하면 훨씬 편하게 긁을 수 있어요!"
                // "나중에 더 단단한 코팅을 긁으려면 더 강한 힘이 필요할 거예요.."
                // "심지어 운도 업그레이드할 수 있답니다. 정말 굉장하죠!"
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
                // "와 새로운 티켓이 준비되었습니다!",
                // "이 티켓은 보상이 훨씬 더 짭짤하지만..",
                // "당첨되려면 운이 좀 더 따라줘야 할 겁니다."
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
                // "사과나무를 긁을 땐 벌레를 조심하세요!",
                // "벌레가 보이면 절대 긁지 마세요!",
                // "벌레가 나와도 완전히 다 긁었을 때만 손해가 발생해요."
                "Be careful of bugs when scraping an apple tree!",
                "If you see a bug, never scratch it!",
                "Even if a bug appears, a loss only occurs once it has completely scratched the item."
            }
        }
    };

    private readonly HashSet<PhoneCallEventType> _triggeredEvents = new HashSet<PhoneCallEventType>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void TriggerEvent(PhoneCallEventType eventType)
    {
        if (phoneCallTrigger == null) return;

        PhoneCallScriptEntry entry = callEntries.Find(e => e.eventType == eventType);
        if (entry == null || entry.lines == null || entry.lines.Length == 0) return;

        if (entry.triggerOnlyOnce && _triggeredEvents.Contains(eventType))
            return;

        if (entry.triggerOnlyOnce)
            _triggeredEvents.Add(eventType);

        phoneCallTrigger.RingPhone(entry.lines);
    }

    // 레벨이 오를 때마다 호출 (예: OnLevelChanged(currentLevel))
    
    // 레벨업 함수에 추가하면 됩니다.
    // PhoneEventController.Instance?.CheckLevelEvents(newLevel);
    // .
    public void CheckLevelEvents(int currentLevel)
    {
        if (currentLevel >= 3) TriggerEvent(PhoneCallEventType.Level3Reached);
        if (currentLevel >= 4) TriggerEvent(PhoneCallEventType.Level4Reached);
        if (currentLevel >= 5) TriggerEvent(PhoneCallEventType.Level5Reached);
        if (currentLevel >= 6) TriggerEvent(PhoneCallEventType.Level6Reached);
        if (currentLevel >= 7) TriggerEvent(PhoneCallEventType.Level7Reached);
    }

    // 깨짐/손해 발생 시 호출
    public void TriggerBrokenOrLossCall()
    {
        TriggerEvent(PhoneCallEventType.FirstLossOrBreak);
    }
}